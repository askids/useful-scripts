public class LimitedModeMonitorService : IHostedService, IDisposable
{
    private Timer _timer;
    private const int RestartDelayMinutes = 15;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Set up a timer that triggers every minute to check the mode status
        _timer = new Timer(CheckLimitedMode, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private void CheckLimitedMode(object state)
    {
        if (Program.IsLimitedMode())
        {
            Console.WriteLine($"Limited mode detected. Scheduling automatic restart in {RestartDelayMinutes} minutes.");
            
            // Reset the timer to trigger a restart after the delay
            _timer?.Change(TimeSpan.FromMinutes(RestartDelayMinutes), Timeout.InfiniteTimeSpan);
            
            // Request a restart due to limited mode
            Program.RequestRestart(RestartReason.Failure);
        }
        else
        {
            // If not in limited mode, keep checking every minute
            _timer?.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
