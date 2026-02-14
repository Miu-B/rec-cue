using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecCue;

/// <summary>
/// Manages multiple <see cref="FileWatcherService"/> instances, one per monitored folder.
/// Aggregates all <see cref="FileWatcherService.FileActivityDetected"/> events into a
/// single <see cref="FileActivityDetected"/> event for consumption by
/// <see cref="RecordingDetectionLogic"/>.
/// </summary>
public class FileWatcherManager : IDisposable
{
    private readonly Dictionary<string, FileWatcherService> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncLock = new();

    /// <summary>
    /// Raised when any managed watcher detects file activity.
    /// </summary>
    public event Action? FileActivityDetected;

    /// <summary>
    /// Synchronise the set of active watchers with the given list of folder paths.
    /// Paths that are new get a watcher created; paths that were removed get their
    /// watcher disposed; unchanged paths are left running.  Empty/whitespace paths
    /// are ignored.  At most <see cref="RecCueConfiguration.MaxFolders"/> entries
    /// are accepted.
    /// </summary>
    public void SyncWatchers(List<string> paths)
    {
        // Normalise: remove blanks, take up to max, deduplicate by full path.
        var desired = paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Path.GetFullPath(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(RecCueConfiguration.MaxFolders)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        lock (_syncLock)
        {
            // Remove watchers for paths no longer in the list.
            var toRemove = _watchers.Keys
                .Where(k => !desired.Contains(k))
                .ToList();

            foreach (var key in toRemove)
            {
                _watchers[key].FileActivityDetected -= OnChildActivity;
                _watchers[key].Dispose();
                _watchers.Remove(key);
            }

            // Add watchers for new paths.
            foreach (var path in desired)
            {
                if (!_watchers.ContainsKey(path))
                {
                    var watcher = new FileWatcherService();
                    watcher.FileActivityDetected += OnChildActivity;
                    watcher.StartMonitoring(path);
                    _watchers[path] = watcher;
                }
            }
        }
    }

    /// <summary>
    /// Stop and dispose all managed watchers.
    /// </summary>
    public void StopAll()
    {
        lock (_syncLock)
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.FileActivityDetected -= OnChildActivity;
                watcher.Dispose();
            }

            _watchers.Clear();
        }
    }

    private void OnChildActivity()
    {
        FileActivityDetected?.Invoke();
    }

    public void Dispose()
    {
        StopAll();
    }
}
