namespace LogVue.Services.Hosting;

/// <summary>
/// Signals when LogVue's tables are ready. The sink is created with the logger, well before the
/// host runs migrations, and events logged in that window must wait rather than fail.
/// </summary>
public sealed class LogVueDatabaseState
{
    private readonly TaskCompletionSource<bool> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes with <c>true</c> once the tables exist, or <c>false</c> if they never will.</summary>
    public Task<bool> Ready => ready.Task;

    public void MarkReady() => ready.TrySetResult(true);

    public void MarkFailed() => ready.TrySetResult(false);
}