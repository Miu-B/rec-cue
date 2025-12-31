using System;
using System.IO;
using System.Linq;
using System.Timers;

namespace RecCue;

public class FileWatcherService : IDisposable
{
    private readonly Timer _pollTimer;
    private string? _monitoredPath;
    private DateTime _lastCheckTime;
    private bool _isMonitoring;

    public event Action? FileActivityDetected;

    public FileWatcherService()
    {
        _pollTimer = new Timer(1000); // 1 second intervals
        _pollTimer.Elapsed += OnPollTimerElapsed;
        _pollTimer.AutoReset = true;
    }

    public void StartMonitoring(string path)
    {
        if (!Directory.Exists(path))
            return;

        _monitoredPath = path;
        _lastCheckTime = DateTime.Now;
        _isMonitoring = true;
        _pollTimer.Start();
    }

    public void StopMonitoring()
    {
        _isMonitoring = false;
        _pollTimer.Stop();
    }

    private void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isMonitoring || string.IsNullOrEmpty(_monitoredPath))
            return;

        try
        {
            // Check if any files in the directory (recursive) have been modified since last check
            var files = Directory.EnumerateFiles(_monitoredPath, "*", SearchOption.AllDirectories);
            var hasActivity = files.Any(file =>
            {
                try
                {
                    return File.GetLastWriteTime(file) > _lastCheckTime;
                }
                catch
                {
                    // Ignore access errors
                    return false;
                }
            });

            if (hasActivity)
            {
                _lastCheckTime = DateTime.Now;
                FileActivityDetected?.Invoke();
            }
        }
        catch
        {
            // Ignore directory access errors
        }
    }

    public void Dispose()
    {
        _pollTimer.Dispose();
    }
}