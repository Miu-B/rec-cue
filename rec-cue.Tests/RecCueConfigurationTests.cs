using RecCue;
using Xunit;

namespace RecCue.Tests;

public class RecCueConfigurationTests : IDisposable
{
    private readonly string _tempDir;

    public RecCueConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"rec-cue-config-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Fact]
    public void DefaultConfiguration_HasExpectedDefaults()
    {
        var config = new RecCueConfiguration();

        Assert.Equal(0, config.Version);
        Assert.Equal("", config.MonitoredFolderPath);
        Assert.Equal(1.0f, config.IndicatorScale);
        Assert.Equal(new System.Numerics.Vector2(300, 300), config.IndicatorPosition);
        Assert.False(config.HideIndicator);
    }

    [Fact]
    public void IsMonitoredFolderValid_EmptyString_ReturnsFalse()
    {
        var config = new RecCueConfiguration { MonitoredFolderPath = "" };

        Assert.False(config.IsMonitoredFolderValid);
    }

    [Fact]
    public void IsMonitoredFolderValid_NullEquivalent_ReturnsFalse()
    {
        var config = new RecCueConfiguration { MonitoredFolderPath = "   " };

        // Whitespace-only path -- directory won't exist
        Assert.False(config.IsMonitoredFolderValid);
    }

    [Fact]
    public void IsMonitoredFolderValid_NonexistentPath_ReturnsFalse()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}")
        };

        Assert.False(config.IsMonitoredFolderValid);
    }

    [Fact]
    public void IsMonitoredFolderValid_ExistingDirectory_ReturnsTrue()
    {
        var config = new RecCueConfiguration { MonitoredFolderPath = _tempDir };

        Assert.True(config.IsMonitoredFolderValid);
    }
}
