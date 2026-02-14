using System;
using System.Timers;

namespace RecCue;

public class RecordingDetectionLogic : IDisposable
{
    private readonly Timer _inactivityTimer;
    private bool _isRecordingActive;
    private readonly object _stateLock = new object();

    public event Action<bool>? RecordingStateChanged;

    public bool IsRecordingActive
    {
        get
        {
            lock (_stateLock)
            {
                return _isRecordingActive;
            }
        }
        private set
        {
            bool changed;
            lock (_stateLock)
            {
                changed = _isRecordingActive != value;
                _isRecordingActive = value;
            }

            if (changed)
            {
                RecordingStateChanged?.Invoke(_isRecordingActive);
            }
        }
    }

    public RecordingDetectionLogic()
    {
        _inactivityTimer = new Timer(5000);
        _inactivityTimer.Elapsed += OnInactivityTimerElapsed;
        _inactivityTimer.AutoReset = false;
    }

    public void OnFileActivityDetected()
    {
        IsRecordingActive = true;
        _inactivityTimer.Stop();
        _inactivityTimer.Start();
    }

    private void OnInactivityTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        IsRecordingActive = false;
    }

    public void Dispose()
    {
        _inactivityTimer.Dispose();
    }
}
