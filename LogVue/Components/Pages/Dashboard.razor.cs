using System.Globalization;
using LogVue.Services.LogEntries;
using LogVue.Services.Query;
using LogVue.Services.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace LogVue.Components.Pages;

public partial class Dashboard : IAsyncDisposable
{
    private static readonly LogEventLevel[] TileLevels =
    [
        LogEventLevel.Fatal,
        LogEventLevel.Error,
        LogEventLevel.Warning,
        LogEventLevel.Information,
    ];

    private DashboardFilter filter = new();
    private LogInsightsDto? insights;
    private LogStoreSummaryDto? summary;
    private bool isLoading;
    private string? loadError;
    private bool leaving;
    private TimeDisplay time = TimeDisplay.Utc;
    private TimeDisplay browserTime = TimeDisplay.Utc;
    private bool showUtc;
    private LogEventLevel minimumLevel;
    private CancellationTokenSource? loadCancellation;
    private DotNetObjectReference<Dashboard>? selfReference;
    private bool disposed;

    [Inject]
    private ILogEntryService LogEntries { get; set; } = default!;

    [Inject]
    private IMinimumLevelService MinimumLevel { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private TimeProvider Clock { get; set; } = default!;

    private DateTime UtcNow => Clock.GetUtcNow().UtcDateTime;

    private long Total => insights?.Levels.Sum(l => l.Count) ?? 0;

    private IReadOnlyList<string> VolumeLabels =>
        insights is null ? [] : [.. insights.Volume.Select(p => BucketLabel(p.BucketUtc))];

    private string WindowCaption
    {
        get
        {
            if (insights is null)
            {
                return filter.Range.Label;
            }

            string count = Total.ToString("N0", CultureInfo.CurrentCulture);
            string events = Total == 1 ? "event" : "events";
            return $"{filter.Range.Label} · {count} {events} · {Rate(Total)}";
        }
    }

    private string BucketCaption
    {
        get
        {
            if (insights is not { } data)
            {
                return "";
            }

            TimeSpan bucket = data.Bucket;

            if (bucket < TimeSpan.FromMinutes(2))
            {
                return "each minute";
            }

            if (bucket < TimeSpan.FromHours(1))
            {
                return $"every {(int)bucket.TotalMinutes} minutes";
            }

            if (bucket < TimeSpan.FromHours(2))
            {
                return "each hour";
            }

            if (bucket < TimeSpan.FromDays(1))
            {
                return $"every {(int)bucket.TotalHours} hours";
            }

            if (bucket < TimeSpan.FromDays(2))
            {
                return "each day";
            }

            return bucket < TimeSpan.FromDays(8) ? "each week" : "each month";
        }
    }

    protected override void OnInitialized()
    {
        var parsed = DashboardFilter.FromUri(Navigation.Uri);

        // Bookmarks from when this address was the search page still carry a query or a level.
        if (!string.IsNullOrWhiteSpace(parsed.Query) || parsed.Levels.Count > 0 || parsed.ExceptionsOnly)
        {
            leaving = true;
            Navigation.NavigateTo(LogsHref(parsed), replace: true);
            return;
        }

        filter = ReadFilter(Navigation.Uri);
        minimumLevel = MinimumLevel.Current;
        selfReference = DotNetObjectReference.Create(this);
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || leaving)
        {
            return;
        }

        var preferences = await Js.InvokeAsync<BrowserPreferences>("logVue.init", selfReference);
        browserTime = TimeDisplay.ForBrowser(preferences.TimeZone);
        showUtc = preferences.Utc;
        time = showUtc ? TimeDisplay.Utc : browserTime;

        await RefreshAsync();
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // NavigationManager is app-wide, so this also runs while the page is going away.
        // Loading after dispose cancels a token source that has already been disposed.
        if (disposed)
        {
            return;
        }

        filter = ReadFilter(e.Location);
        await InvokeAsync(LoadAsync);
    }

    private void ApplyFilter(DashboardFilter next) =>
        Navigation.NavigateTo(next.ToUri(Navigation, includeAllRange: true));

    private async Task RefreshAsync() => await Task.WhenAll(LoadAsync(), LoadSummaryAsync());

    private async Task LoadAsync()
    {
        if (disposed)
        {
            return;
        }

        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        loadCancellation = new CancellationTokenSource();
        var cancellationToken = loadCancellation.Token;

        isLoading = true;
        StateHasChanged();

        try
        {
            var result = await LogEntries.GetInsightsAsync(filter.ToLogFilter(UtcNow), cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (result.IsSuccess)
            {
                insights = result.Value;
                loadError = null;
            }
            else
            {
                insights = null;
                loadError = result.GetDisplayMessage();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            isLoading = false;
        }

        if (!disposed)
        {
            StateHasChanged();
        }
    }

    private async Task LoadSummaryAsync()
    {
        var result = await LogEntries.GetSummaryAsync();
        summary = result.IsSuccess ? result.Value : summary;
        StateHasChanged();
    }

    private async Task ToggleUtcAsync()
    {
        showUtc = !showUtc;
        time = showUtc ? TimeDisplay.Utc : browserTime;
        await Js.InvokeVoidAsync("logVue.setUtc", showUtc);
    }

    /// <summary>A missing range means the default window. <c>range=all</c> is an explicit choice.</summary>
    private static DashboardFilter ReadFilter(string uri)
    {
        var filter = DashboardFilter.FromUri(uri);
        bool hasRange = QueryHelpers.ParseQuery(new Uri(uri).Query).ContainsKey("range");
        return hasRange ? filter : filter with { Range = TimeRange.Find("24h") };
    }

    private string LogsHref(DashboardFilter next)
    {
        string withQuery = next.ToUri(Navigation, includeAllRange: true);
        int queryIndex = withQuery.IndexOf('?', StringComparison.Ordinal);
        string query = queryIndex >= 0 ? withQuery[queryIndex..] : string.Empty;
        return "logs" + query;
    }

    private string LevelHref(LogEventLevel level) =>
        LogsHref(filter with { Levels = new HashSet<LogEventLevel> { level }, Query = null, ExceptionsOnly = false });

    private string ExceptionsHref() =>
        LogsHref(filter with { Levels = new HashSet<LogEventLevel>(), Query = null, ExceptionsOnly = true });

    private string TemplateHref(string template) =>
        LogsHref(filter with
        {
            Query = LogQueryText.Condition("MessageTemplate", template),
            Levels = new HashSet<LogEventLevel>(),
            ExceptionsOnly = false,
        });

    private long CountOf(LogEventLevel level) =>
        insights?.Levels.FirstOrDefault(l => l.Level == level)?.Count ?? 0;

    private long? PreviousCount(LogEventLevel level) =>
        insights?.PreviousLevels?.FirstOrDefault(l => l.Level == level)?.Count;

    private string BucketLabel(DateTime utc)
    {
        DateTime local = time.ToDisplay(utc);
        TimeSpan span = insights is null ? TimeSpan.Zero : insights.ToUtc - insights.FromUtc;

        if (insights?.Bucket >= TimeSpan.FromDays(28))
        {
            return local.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        }

        if (insights?.Bucket >= TimeSpan.FromDays(1))
        {
            return local.ToString("d MMM", CultureInfo.InvariantCulture);
        }

        return span > TimeSpan.FromDays(1)
            ? local.ToString("d MMM HH:mm", CultureInfo.InvariantCulture)
            : local.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private string Rate(long count)
    {
        if (insights is null)
        {
            return "";
        }

        TimeSpan span = insights.ToUtc - insights.FromUtc;

        if (span.TotalHours >= 48)
        {
            return $"{count / Math.Max(span.TotalDays, 1):0.#} / day";
        }

        if (span.TotalMinutes >= 180)
        {
            return $"{count / Math.Max(span.TotalHours, 1):0.#} / hour";
        }

        return $"{count / Math.Max(span.TotalMinutes, 1):0.#} / min";
    }

    private static string? Delta(long current, long? previous)
    {
        if (previous is not long before)
        {
            return null;
        }

        if (before == 0 && current == 0)
        {
            return "no change";
        }

        if (before == 0)
        {
            return "none in the previous window";
        }

        double percent = (current - before) * 100d / before;
        string arrow = percent > 0.5 ? "↑" : percent < -0.5 ? "↓" : "→";
        return $"{arrow} {Math.Abs(percent):0}% vs previous";
    }

    private static string DeltaClass(long current, long? previous, bool increaseIsBad)
    {
        if (previous is not long before || before == 0 || current == before)
        {
            return "lv-muted";
        }

        bool increased = current > before;
        return increased == increaseIsBad ? "lv-text-danger" : "lv-text-good";
    }

    private static bool IncreaseIsBad(LogEventLevel level) =>
        level is LogEventLevel.Fatal or LogEventLevel.Error or LogEventLevel.Warning;

    [JSInvokable]
    public void OnShortcut(string key)
    {
        _ = key;
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        Navigation.LocationChanged -= OnLocationChanged;
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        selfReference?.Dispose();
        await ValueTask.CompletedTask;
    }

    private sealed record BrowserPreferences(string? TimeZone, bool Utc);
}
