using System;
using System.IO;

namespace RecCue;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly object _stateLock = new object();
    private bool _isMonitoring;

    public event Action? FileActivityDetected;

    public void StartMonitoring(string path)
    {
        if (!Directory.Exists(path))
            return;

        lock (_stateLock)
        {
            StopMonitoringInternal();

            _watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = true,
                Filter = "*",
                EnableRaisingEvents = true,
            };

            _watcher.Changed += OnFileEvent;
            _watcher.Created += OnFileEvent;
            _watcher.Error += OnWatcherError;

            _isMonitoring = true;
        }
    }

    public void StopMonitoring()
    {
        lock (_stateLock)
        {
            StopMonitoringInternal();
        }
    }

    private void StopMonitoringInternal()
    {
        _isMonitoring = false;

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileEvent;
            _watcher.Created -= OnFileEvent;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        lock (_stateLock)
        {
            if (!_isMonitoring)
                return;
        }

        FileActivityDetected?.Invoke();
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // Attempt to recover by restarting the watcher on the same path.
        string? path;
        lock (_stateLock)
        {
            path = _watcher?.Path;
            StopMonitoringInternal();
        }

        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            StartMonitoring(path);
        }
    }

    public void Dispose()
    {
        lock (_stateLock)
        {
            StopMonitoringInternal();
        }
    }
}
