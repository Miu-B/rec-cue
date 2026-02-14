using System;
using System.IO;
using System.Linq;
using System.Timers;

namespace RecCue;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private Timer? _pollTimer;
    private readonly object _stateLock = new object();
    private bool _isMonitoring;

    // Last check time for the directory-level mtime scan.
    // Reset each time activity is found; used to compare against
    // file LastWriteTime values during polling.
    private DateTime _lastCheckTime;
    private int _lastFileCount;

    /// <summary>
    /// Interval in milliseconds between directory-scan poll checks.
    /// Exposed for testing.
    /// </summary>
    public double PollIntervalMs { get; set; } = 1000;

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

            _lastCheckTime = DateTime.UtcNow;
            _lastFileCount = 0;
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

        if (_pollTimer != null)
        {
            _pollTimer.Stop();
            _pollTimer.Elapsed -= OnPollTimerElapsed;
            _pollTimer.Dispose();
            _pollTimer = null;
        }

        _lastCheckTime = DateTime.MinValue;
        _lastFileCount = 0;

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

            _lastCheckTime = DateTime.UtcNow;
            EnsurePollTimerRunning();
        }

        FileActivityDetected?.Invoke();
    }

    private void EnsurePollTimerRunning()
    {
        // Must be called under _stateLock.
        if (_pollTimer == null)
        {
            _pollTimer = new Timer(PollIntervalMs);
            _pollTimer.AutoReset = true;
            _pollTimer.Elapsed += OnPollTimerElapsed;
        }

        // Reset the timer so the next tick is a full interval from now.
        _pollTimer.Stop();
        _pollTimer.Start();
    }

    private void OnPollTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        string? monitoredPath;
        DateTime lastCheck;

        lock (_stateLock)
        {
            if (!_isMonitoring || _watcher == null)
            {
                _pollTimer?.Stop();
                return;
            }

            monitoredPath = _watcher.Path;
            lastCheck = _lastCheckTime;
        }

        if (string.IsNullOrEmpty(monitoredPath) || !Directory.Exists(monitoredPath))
        {
            lock (_stateLock)
            {
                _pollTimer?.Stop();
            }
            return;
        }

        try
        {
            var files = Directory.EnumerateFiles(monitoredPath, "*", SearchOption.AllDirectories).ToArray();
            var hasActivity = false;

            // Signal 1: file count changed (new file created or deleted).
            if (files.Length != _lastFileCount)
            {
                hasActivity = true;
            }
            else
            {
                // Signal 2: any file has been written to since our last check.
                foreach (var file in files)
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(file) > lastCheck)
                        {
                            hasActivity = true;
                            break;
                        }
                    }
                    catch
                    {
                        // File may have been deleted between enumeration and check.
                    }
                }
            }

            if (hasActivity)
            {
                lock (_stateLock)
                {
                    _lastCheckTime = DateTime.UtcNow;
                    _lastFileCount = files.Length;
                }

                FileActivityDetected?.Invoke();
            }
            else
            {
                // No activity detected — stop polling.
                // The inactivity timer in RecordingDetectionLogic will
                // handle turning off the indicator after its timeout.
                lock (_stateLock)
                {
                    _pollTimer?.Stop();
                }
            }
        }
        catch
        {
            // Directory may have been deleted or become inaccessible.
            lock (_stateLock)
            {
                _pollTimer?.Stop();
            }
        }
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
