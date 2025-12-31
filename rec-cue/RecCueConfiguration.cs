using Dalamud.Configuration;
using System;
using System.Numerics;

namespace RecCue;

[Serializable]
public class RecCueConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string MonitoredFolderPath { get; set; } = string.Empty;
    public float IndicatorScale { get; set; } = 1.0f;
    public Vector2 IndicatorPosition { get; set; } = new Vector2(300, 300);

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        RecCuePlugin.PluginInterface.SavePluginConfig(this);
    }
}
