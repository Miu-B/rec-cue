using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.ImGuiFileDialog;

namespace RecCue.Windows;

public class RecCueConfigWindow : Window, IDisposable
{
    private readonly RecCuePlugin plugin;
    private readonly RecCueConfiguration configuration;
    private FileDialog? _folderDialog;

    public RecCueConfigWindow(RecCuePlugin plugin) : base("Rec-Cue Configuration")
    {
        this.plugin = plugin;
        configuration = plugin.Configuration;

        Size = new Vector2(400, 200);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Monitored Folder:");
        var folderPath = configuration.MonitoredFolderPath;
        if (ImGui.InputText("##FolderPath", ref folderPath, 500))
        {
            configuration.MonitoredFolderPath = folderPath;
            configuration.Save();
            plugin.StartMonitoring();
        }

        ImGui.SameLine();
        if (ImGui.Button("Browse..."))
        {
            _folderDialog = new FileDialog("Select Folder", ".", "", ".cs", "", "", 0, false, ImGuiFileDialogFlags.None);
            _folderDialog?.Show();
        }

        if (!System.IO.Directory.Exists(configuration.MonitoredFolderPath))
        {
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Warning: Folder does not exist!");
        }

        ImGui.Spacing();

        ImGui.Text("Indicator Scale:");
        var scale = configuration.IndicatorScale;
        if (ImGui.SliderFloat("##Scale", ref scale, 0.5f, 2.0f, "%.1fx"))
        {
            configuration.IndicatorScale = scale;
            configuration.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Close"))
        {
            IsOpen = false;
        }

        if (_folderDialog != null)
        {
            var dialogAction = _folderDialog.Draw();
            if (dialogAction)
            {
                if (_folderDialog.GetIsOk())
                {
                    var results = _folderDialog.GetResults();
                    if (results.Count > 0)
                    {
                        configuration.MonitoredFolderPath = results[0];
                        configuration.Save();
                        plugin.StartMonitoring();
                    }
                }
                _folderDialog = null;
            }
        }
    }
}