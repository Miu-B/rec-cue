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
    private readonly object _stateLock = new object();
    private DateTime _lastFileCountCheck;
    private int _lastFileCount;

    public event Action? FileActivityDetected;

    public FileWatcherService()
    {
        _pollTimer = new Timer(1000);
        _pollTimer.Elapsed += OnPollTimerElapsed;
        _pollTimer.AutoReset = true;
    }

    public void StartMonitoring(string path)
    {
        if (!Directory.Exists(path))
            return;

        lock (_stateLock)
        {
            _monitoredPath = path;
            _lastCheckTime = DateTime.Now;
            _lastFileCountCheck = DateTime.Now;
            _lastFileCount = 0;
            _isMonitoring = true;
            _pollTimer.Start();
        }
    }

    public void StopMonitoring()
    {
        lock (_stateLock)
        {
            _isMonitoring = false;
            _pollTimer.Stop();
        }
    }

    private void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        string? path;
        bool isMonitoring;
        DateTime lastCheck;

        lock (_stateLock)
        {
            path = _monitoredPath;
            isMonitoring = _isMonitoring;
            lastCheck = _lastCheckTime;
        }

        if (!isMonitoring || string.IsNullOrEmpty(path))
            return;

        try
        {
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray();
            var hasActivity = false;

            if (files.Length != _lastFileCount)
            {
                hasActivity = true;
            }
            else
            {
                foreach (var file in files)
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) > lastCheck)
                        {
                            hasActivity = true;
                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            if (hasActivity)
            {
                lock (_stateLock)
                {
                    _lastCheckTime = DateTime.Now;
                    _lastFileCount = files.Length;
                }
                FileActivityDetected?.Invoke();
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _pollTimer.Dispose();
    }
}