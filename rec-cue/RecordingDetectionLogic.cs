using System;
using System.Timers;

namespace RecCue;

public class RecordingDetectionLogic : IDisposable
{
    private readonly Timer _inactivityTimer;
    private bool _isRecordingActive;

    public event Action<bool>? RecordingStateChanged;

    public bool IsRecordingActive
    {
        get => _isRecordingActive;
        private set
        {
            if (_isRecordingActive != value)
            {
                _isRecordingActive = value;
                RecordingStateChanged?.Invoke(_isRecordingActive);
            }
        }
    }

    public RecordingDetectionLogic()
    {
        _inactivityTimer = new Timer(3000); // 3 seconds
        _inactivityTimer.Elapsed += OnInactivityTimerElapsed;
        _inactivityTimer.AutoReset = false; // One-shot
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
