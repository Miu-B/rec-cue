using RecCue;
using Xunit;

namespace RecCue.Tests;

public class FileWatcherServiceTests : IDisposable
{
    private readonly FileWatcherService _watcher;
    private readonly string _tempDir;

    public FileWatcherServiceTests()
    {
        _watcher = new FileWatcherService();
        _tempDir = Path.Combine(Path.GetTempPath(), $"rec-cue-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _watcher.Dispose();

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
    public void StartMonitoring_ValidPath_Succeeds()
    {
        // Should not throw
        _watcher.StartMonitoring(_tempDir);
    }

    [Fact]
    public void StartMonitoring_NonexistentPath_DoesNotThrow()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}");

        // Should not throw, just silently ignore
        _watcher.StartMonitoring(fakePath);
    }

    [Fact]
    public async Task FileCreated_FiresFileActivityDetected()
    {
        var activityDetected = new TaskCompletionSource<bool>();
        _watcher.FileActivityDetected += () => activityDetected.TrySetResult(true);

        _watcher.StartMonitoring(_tempDir);

        // Give the watcher a moment to initialize
        await Task.Delay(200);

        // Create a file in the monitored directory
        var testFile = Path.Combine(_tempDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var result = await Task.WhenAny(activityDetected.Task, Task.Delay(5000));
        Assert.True(activityDetected.Task.IsCompleted, "FileActivityDetected was not raised after file creation");
    }

    [Fact]
    public async Task FileModified_FiresFileActivityDetected()
    {
        // Create file before monitoring starts
        var testFile = Path.Combine(_tempDir, "existing.txt");
        await File.WriteAllTextAsync(testFile, "initial content");

        var activityDetected = new TaskCompletionSource<bool>();
        _watcher.FileActivityDetected += () => activityDetected.TrySetResult(true);

        _watcher.StartMonitoring(_tempDir);

        // Give the watcher a moment to initialize
        await Task.Delay(200);

        // Modify the file
        await File.WriteAllTextAsync(testFile, "modified content");

        var result = await Task.WhenAny(activityDetected.Task, Task.Delay(5000));
        Assert.True(activityDetected.Task.IsCompleted, "FileActivityDetected was not raised after file modification");
    }

    [Fact]
    public async Task SubdirectoryFileCreated_FiresFileActivityDetected()
    {
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        var activityDetected = new TaskCompletionSource<bool>();
        _watcher.FileActivityDetected += () => activityDetected.TrySetResult(true);

        _watcher.StartMonitoring(_tempDir);

        // Give the watcher a moment to initialize
        await Task.Delay(200);

        // Create a file in the subdirectory
        var testFile = Path.Combine(subDir, "nested.txt");
        await File.WriteAllTextAsync(testFile, "nested content");

        var result = await Task.WhenAny(activityDetected.Task, Task.Delay(5000));
        Assert.True(activityDetected.Task.IsCompleted, "FileActivityDetected was not raised for subdirectory file creation");
    }

    [Fact]
    public async Task StopMonitoring_PreventsEvents()
    {
        var eventCount = 0;
        _watcher.FileActivityDetected += () => Interlocked.Increment(ref eventCount);

        _watcher.StartMonitoring(_tempDir);
        await Task.Delay(200);

        _watcher.StopMonitoring();
        await Task.Delay(200);

        // Create a file after stopping -- should not fire
        var testFile = Path.Combine(_tempDir, "after-stop.txt");
        await File.WriteAllTextAsync(testFile, "should not trigger");

        await Task.Delay(1000);
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void StopMonitoring_WhenNotStarted_DoesNotThrow()
    {
        // Should not throw
        _watcher.StopMonitoring();
    }

    [Fact]
    public void Dispose_WhenMonitoring_DoesNotThrow()
    {
        _watcher.StartMonitoring(_tempDir);

        // Should not throw
        _watcher.Dispose();
    }
}
