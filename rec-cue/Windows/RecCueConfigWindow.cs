using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.ImGuiFileDialog;

namespace RecCue.Windows;

public class RecCueConfigWindow : Window, IDisposable
{
    private readonly RecCuePlugin plugin;
    private readonly RecCueConfiguration configuration;

    // Per-row folder dialog tracking.
    private FileDialog? _folderDialog;
    private int _folderDialogRow = -1;

    // Local editable copy of paths so we can add/remove rows independently
    // of the persisted config (empty rows are slots, cleaned on save).
    private List<string> _editPaths = new();

    public RecCueConfigWindow(RecCuePlugin plugin) : base("Rec-Cue Configuration")
    {
        this.plugin = plugin;
        configuration = plugin.Configuration;

        Size = new Vector2(450, 320);
        SizeCondition = ImGuiCond.FirstUseEver;

        SyncEditPaths();
    }

    public void Dispose()
    {
        _folderDialog = null;
    }

    /// <summary>
    /// Copy the current config paths into the local editable list,
    /// ensuring there is always at least one row visible.
    /// </summary>
    private void SyncEditPaths()
    {
        _editPaths = new List<string>(configuration.MonitoredFolderPaths);
        if (_editPaths.Count == 0)
            _editPaths.Add(string.Empty);
    }

    /// <summary>
    /// Persist the current edit paths back to config and re-sync watchers.
    /// </summary>
    private void ApplyAndSave()
    {
        configuration.MonitoredFolderPaths = new List<string>(_editPaths);
        configuration.Save();
        plugin.StartMonitoring();
    }

    public override void Draw()
    {
        // --- Monitored Folders section ---
        ImGui.Text("Monitored Folders:");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), $"({_editPaths.Count}/{RecCueConfiguration.MaxFolders})");

        ImGui.Spacing();

        var changed = false;
        var removeIndex = -1;

        for (var i = 0; i < _editPaths.Count; i++)
        {
            ImGui.PushID(i);

            var path = _editPaths[i];

            // Text input — use remaining width minus space for Browse + X buttons.
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 120);
            if (ImGui.InputText("##Path", ref path, 500))
            {
                _editPaths[i] = path;
                changed = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Browse"))
            {
                _folderDialogRow = i;
                _folderDialog = new FileDialog("Select Folder", ".", "", ".cs", "", "", 0, false, ImGuiFileDialogFlags.None);
                _folderDialog?.Show();
            }

            ImGui.SameLine();
            if (ImGui.Button("X"))
            {
                removeIndex = i;
            }

            // Per-entry validation: warn if non-empty but directory doesn't exist.
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            {
                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "  Folder does not exist!");
            }

            ImGui.PopID();
        }

        // Handle row removal.
        if (removeIndex >= 0)
        {
            _editPaths.RemoveAt(removeIndex);
            if (_editPaths.Count == 0)
                _editPaths.Add(string.Empty);
            changed = true;
        }

        // "Add Folder" button (disabled at max).
        ImGui.Spacing();
        var atMax = _editPaths.Count >= RecCueConfiguration.MaxFolders;
        if (atMax)
            ImGui.BeginDisabled();
        if (ImGui.Button("+ Add Folder"))
        {
            _editPaths.Add(string.Empty);
        }
        if (atMax)
            ImGui.EndDisabled();

        if (changed)
            ApplyAndSave();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // --- Indicator Scale ---
        ImGui.Text("Indicator Scale:");
        var scale = configuration.IndicatorScale;
        if (ImGui.SliderFloat("##Scale", ref scale, 0.5f, 2.0f, "%.1fx"))
        {
            configuration.IndicatorScale = scale;
            configuration.Save();
        }

        ImGui.Spacing();

        // --- Visibility ---
        ImGui.Text("Visibility:");
        var hideIndicator = configuration.HideIndicator;
        if (ImGui.Checkbox("Hide Indicator##HideIndicator", ref hideIndicator))
        {
            configuration.HideIndicator = hideIndicator;
            configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Close"))
        {
            IsOpen = false;
        }

        // --- Folder dialog draw / result handling ---
        if (_folderDialog != null)
        {
            var dialogAction = _folderDialog.Draw();
            if (dialogAction)
            {
                if (_folderDialog.GetIsOk())
                {
                    var results = _folderDialog.GetResults();
                    if (results.Count > 0 && _folderDialogRow >= 0 && _folderDialogRow < _editPaths.Count)
                    {
                        _editPaths[_folderDialogRow] = results[0];
                        ApplyAndSave();
                    }
                }
                _folderDialog = null;
                _folderDialogRow = -1;
            }
        }
    }
}
