using System.Diagnostics.Metrics;
using System.Timers;

namespace minstances.Services;

public interface IBlueskyService
{
    event EventHandler<EventArgs> BlueskyEvent;
    void CreateTimerEvent(double interval);
    void StartTimer();
    void StopTimer();
}

public class BlueskyService : IBlueskyService
{   
    private System.Timers.Timer _timer;

    // define the event
    public event EventHandler<EventArgs> BlueskyEvent;

    private int _counter = 0;

    public BlueskyService()
    {

    }

    public void CreateTimerEvent(double interval)
    {
        _timer = new System.Timers.Timer(interval);
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    public void StartTimer()
    {
        _timer.Start();

    }

    public void StopTimer()
    {
        _timer.Stop();
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        _counter++;
        // Raise the event
        BlueskyEvent?.Invoke(_counter, e);
    }
}
