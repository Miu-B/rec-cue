using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace RecCue;

[Serializable]
public class RecCueConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>
    /// Legacy single-folder path.  Kept for backward-compatible deserialization
    /// so that existing configs migrate cleanly.  Not used at runtime after migration.
    /// </summary>
    public string MonitoredFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Up to 5 monitored folder paths.  Empty strings are ignored (unused slots).
    /// </summary>
    public List<string> MonitoredFolderPaths { get; set; } = new();

    public float IndicatorScale { get; set; } = 1.0f;
    public Vector2 IndicatorPosition { get; set; } = new Vector2(300, 300);
    public bool HideIndicator { get; set; } = false;

    public const int MaxFolders = 5;

    /// <summary>
    /// Returns true if the given path is a non-empty string pointing to an existing directory.
    /// </summary>
    public static bool IsPathValid(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);

    /// <summary>
    /// Returns true if at least one monitored folder path is valid.
    /// </summary>
    public bool HasAnyValidMonitoredFolder =>
        MonitoredFolderPaths.Any(p => IsPathValid(p));

    /// <summary>
    /// Returns true if any non-empty configured folder does NOT exist on disk.
    /// Used to show the orange warning indicator.
    /// </summary>
    public bool HasAnyInvalidNonEmptyFolder =>
        MonitoredFolderPaths.Any(p => !string.IsNullOrWhiteSpace(p) && !Directory.Exists(p));

    /// <summary>
    /// Kept for backward compatibility.  Delegates to the new list.
    /// </summary>
    public bool IsMonitoredFolderValid => HasAnyValidMonitoredFolder;

    /// <summary>
    /// Migrate from Version 0 (single folder) to Version 1 (multi-folder list).
    /// Call this once after deserialization.
    /// </summary>
    public void Migrate()
    {
        if (Version == 0)
        {
            if (!string.IsNullOrWhiteSpace(MonitoredFolderPath) && MonitoredFolderPaths.Count == 0)
            {
                MonitoredFolderPaths.Add(MonitoredFolderPath);
            }

            Version = 1;
        }
    }

    /// <summary>
    /// Remove empty/whitespace-only entries and enforce the max folder limit.
    /// Call before saving to keep the persisted config tidy.
    /// </summary>
    public void CleanPaths()
    {
        MonitoredFolderPaths = MonitoredFolderPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Take(MaxFolders)
            .ToList();
    }

    public void Save()
    {
        CleanPaths();
        RecCuePlugin.PluginInterface.SavePluginConfig(this);
    }
}
