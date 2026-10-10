using System.Collections.Concurrent;

namespace LogVue.Infrastructure;

/// <summary>
/// Transient notifications, rendered once by <c>Toaster</c> on the dashboard.
/// </summary>
public sealed class ToastService : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Toast> toasts = new();
    private readonly ConcurrentDictionary<Guid, Timer> timers = new();

    public event Action? Changed;

    public IReadOnlyCollection<Toast> Toasts => [.. toasts.Values.OrderBy(t => t.CreatedAt)];

    public void Success(string message) => Show(message, ToastLevel.Success);

    public void Error(string message) => Show(message, ToastLevel.Error, TimeSpan.FromSeconds(12));

    public void Info(string message) => Show(message, ToastLevel.Info);

    /// <summary>Surfaces a failed result without every caller writing the same status switch.</summary>
    public void ShowFailure(Ardalis.Result.IResult result) => Error(result.GetDisplayMessage());

    public void Dismiss(Guid id)
    {
        toasts.TryRemove(id, out _);

        if (timers.TryRemove(id, out var timer))
        {
            timer.Dispose();
        }

        Changed?.Invoke();
    }

    public void Dispose()
    {
        foreach (var timer in timers.Values)
        {
            timer.Dispose();
        }

        timers.Clear();
        toasts.Clear();
    }

    private void Show(string message, ToastLevel level, TimeSpan? duration = null)
    {
        var toast = new Toast(Guid.NewGuid(), message, level, DateTime.UtcNow);
        toasts[toast.Id] = toast;

        timers[toast.Id] = new Timer(
            _ => Dismiss(toast.Id),
            null,
            duration ?? TimeSpan.FromSeconds(5),
            Timeout.InfiniteTimeSpan);

        Changed?.Invoke();
    }

    public enum ToastLevel
    {
        Success,
        Error,
        Info,
    }

    public sealed record Toast(Guid Id, string Message, ToastLevel Level, DateTime CreatedAt);
}
