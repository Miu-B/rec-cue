using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;
using RecCue.Windows;

namespace RecCue;

public sealed class RecCuePlugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;

    private const string CommandName = "/reccue";

    public RecCueConfiguration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("RecCue");
    internal RecCueConfigWindow ConfigWindow { get; init; }
    internal RecIndicator Indicator { get; init; }
    public FileWatcherManager FileWatcherManager { get; private set; } = null!;
    public RecordingDetectionLogic RecordingLogic { get; private set; } = null!;

    // Crisp font for the indicator (resized default font)
    internal IFontHandle RecIndicatorFont { get; private set; } = null!;
    private bool _ownsRecIndicatorFont;
    private float _lastFontMultiplier = -1f;

    public RecCuePlugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as RecCueConfiguration ?? new RecCueConfiguration();
        Configuration.Migrate();

        FileWatcherManager = new FileWatcherManager();
        RecordingLogic = new RecordingDetectionLogic();

        FileWatcherManager.FileActivityDetected += RecordingLogic.OnFileActivityDetected;

        ConfigWindow = new RecCueConfigWindow(this);
        Indicator = new RecIndicator(this, ClientState, Condition);

        // Create font handle for sharp indicator text
        EnsureRecIndicatorFont();

        if (Configuration.MonitoredFolderPaths.Count > 0)
            StartMonitoring();

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show/hide the indicator or open the configuration window. Options: show, hide"
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        Log.Information("===rec-cue plugin loaded successfully===");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        Indicator.Dispose();

        FileWatcherManager.Dispose();
        RecordingLogic.Dispose();

        CommandManager.RemoveHandler(CommandName);

        if (_ownsRecIndicatorFont)
            RecIndicatorFont.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var trimmedArgs = args.Trim().ToLower();

        if (trimmedArgs == "show")
        {
            Configuration.HideIndicator = false;
            Configuration.Save();
            Log.Information("Indicator shown");
        }
        else if (trimmedArgs == "hide")
        {
            Configuration.HideIndicator = true;
            Configuration.Save();
            Log.Information("Indicator hidden");
        }
        else
        {
            ConfigWindow.Toggle();
        }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void StartMonitoring()
    {
        FileWatcherManager.SyncWatchers(Configuration.MonitoredFolderPaths);
    }

    private void DrawUi()
    {
        // Rebuild font if user changed IndicatorScale in your config
        EnsureRecIndicatorFont();

        WindowSystem.Draw();
        Indicator.Draw();
    }

    private void EnsureRecIndicatorFont()
    {
        var multiplier = 1.8f * Configuration.IndicatorScale;

        if (RecIndicatorFont != null && MathF.Abs(multiplier - _lastFontMultiplier) < 0.01f)
            return;

        if (_ownsRecIndicatorFont && RecIndicatorFont != null)
        {
            RecIndicatorFont.Dispose();
            _ownsRecIndicatorFont = false;
        }

        _lastFontMultiplier = multiplier;

        if (PluginInterface.UiBuilder.DefaultFontSpec is SingleFontSpec spec)
        {
            var scaledSpec = spec with { SizePx = spec.SizePx * multiplier };
            RecIndicatorFont = scaledSpec.CreateFontHandle(PluginInterface.UiBuilder.FontAtlas);
            _ownsRecIndicatorFont = true;
        }
        else
        {
            RecIndicatorFont = PluginInterface.UiBuilder.DefaultFontHandle;
            _ownsRecIndicatorFont = false;
        }
    }
}

