using RecCue;
using Xunit;

namespace RecCue.Tests;

public class RecCueConfigurationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempDir2;

    public RecCueConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"rec-cue-config-test-{Guid.NewGuid():N}");
        _tempDir2 = Path.Combine(Path.GetTempPath(), $"rec-cue-config-test2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_tempDir2);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { }

        try
        {
            if (Directory.Exists(_tempDir2))
                Directory.Delete(_tempDir2, recursive: true);
        }
        catch { }
    }

    [Fact]
    public void DefaultConfiguration_HasExpectedDefaults()
    {
        var config = new RecCueConfiguration();

        Assert.Equal(1, config.Version);
        Assert.Equal("", config.MonitoredFolderPath);
        Assert.Empty(config.MonitoredFolderPaths);
        Assert.Equal(1.0f, config.IndicatorScale);
        Assert.Equal(new System.Numerics.Vector2(300, 300), config.IndicatorPosition);
        Assert.False(config.HideIndicator);
    }

    // --- IsPathValid static helper ---

    [Fact]
    public void IsPathValid_EmptyString_ReturnsFalse()
    {
        Assert.False(RecCueConfiguration.IsPathValid(""));
    }

    [Fact]
    public void IsPathValid_Null_ReturnsFalse()
    {
        Assert.False(RecCueConfiguration.IsPathValid(null));
    }

    [Fact]
    public void IsPathValid_Whitespace_ReturnsFalse()
    {
        Assert.False(RecCueConfiguration.IsPathValid("   "));
    }

    [Fact]
    public void IsPathValid_NonexistentPath_ReturnsFalse()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}");
        Assert.False(RecCueConfiguration.IsPathValid(fakePath));
    }

    [Fact]
    public void IsPathValid_ExistingDirectory_ReturnsTrue()
    {
        Assert.True(RecCueConfiguration.IsPathValid(_tempDir));
    }

    // --- HasAnyValidMonitoredFolder ---

    [Fact]
    public void HasAnyValidMonitoredFolder_EmptyList_ReturnsFalse()
    {
        var config = new RecCueConfiguration();
        Assert.False(config.HasAnyValidMonitoredFolder);
    }

    [Fact]
    public void HasAnyValidMonitoredFolder_AllInvalid_ReturnsFalse()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { "/nonexistent/path", "   " }
        };
        Assert.False(config.HasAnyValidMonitoredFolder);
    }

    [Fact]
    public void HasAnyValidMonitoredFolder_MixedValidInvalid_ReturnsTrue()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { "/nonexistent/path", _tempDir }
        };
        Assert.True(config.HasAnyValidMonitoredFolder);
    }

    // --- HasAnyInvalidNonEmptyFolder ---

    [Fact]
    public void HasAnyInvalidNonEmptyFolder_EmptyList_ReturnsFalse()
    {
        var config = new RecCueConfiguration();
        Assert.False(config.HasAnyInvalidNonEmptyFolder);
    }

    [Fact]
    public void HasAnyInvalidNonEmptyFolder_AllValid_ReturnsFalse()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { _tempDir, _tempDir2 }
        };
        Assert.False(config.HasAnyInvalidNonEmptyFolder);
    }

    [Fact]
    public void HasAnyInvalidNonEmptyFolder_OneInvalidNonEmpty_ReturnsTrue()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { _tempDir, "/does/not/exist" }
        };
        Assert.True(config.HasAnyInvalidNonEmptyFolder);
    }

    [Fact]
    public void HasAnyInvalidNonEmptyFolder_EmptyEntries_Ignored()
    {
        // Empty/whitespace entries are unused slots, NOT errors.
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { _tempDir, "", "   " }
        };
        Assert.False(config.HasAnyInvalidNonEmptyFolder);
    }

    // --- Migration ---

    [Fact]
    public void Migrate_V0WithSingleFolder_MovesToList()
    {
        var config = new RecCueConfiguration
        {
            Version = 0,
            MonitoredFolderPath = "/some/path"
        };

        config.Migrate();

        Assert.Equal(1, config.Version);
        Assert.Single(config.MonitoredFolderPaths);
        Assert.Equal("/some/path", config.MonitoredFolderPaths[0]);
    }

    [Fact]
    public void Migrate_V0WithEmptyFolder_DoesNotAddEmptyEntry()
    {
        var config = new RecCueConfiguration
        {
            Version = 0,
            MonitoredFolderPath = ""
        };

        config.Migrate();

        Assert.Equal(1, config.Version);
        Assert.Empty(config.MonitoredFolderPaths);
    }

    [Fact]
    public void Migrate_V1_NoOp()
    {
        var config = new RecCueConfiguration
        {
            Version = 1,
            MonitoredFolderPaths = new() { "/a", "/b" }
        };

        config.Migrate();

        Assert.Equal(1, config.Version);
        Assert.Equal(2, config.MonitoredFolderPaths.Count);
    }

    // --- CleanPaths ---

    [Fact]
    public void CleanPaths_RemovesEmptyAndWhitespace()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { "/a", "", "   ", "/b" }
        };

        config.CleanPaths();

        Assert.Equal(2, config.MonitoredFolderPaths.Count);
        Assert.Equal("/a", config.MonitoredFolderPaths[0]);
        Assert.Equal("/b", config.MonitoredFolderPaths[1]);
    }

    [Fact]
    public void CleanPaths_EnforcesMaxFolders()
    {
        var config = new RecCueConfiguration
        {
            MonitoredFolderPaths = new() { "/a", "/b", "/c", "/d", "/e", "/f", "/g" }
        };

        config.CleanPaths();

        Assert.Equal(RecCueConfiguration.MaxFolders, config.MonitoredFolderPaths.Count);
    }
}
