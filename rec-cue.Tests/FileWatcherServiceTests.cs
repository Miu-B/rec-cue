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

    [Fact]
    public async Task ContinuousWrites_SustainFileActivityViaPolling()
    {
        // Use a short poll interval for faster testing.
        _watcher.PollIntervalMs = 500;

        var eventCount = 0;
        _watcher.FileActivityDetected += () => Interlocked.Increment(ref eventCount);

        _watcher.StartMonitoring(_tempDir);
        await Task.Delay(200);

        // Create a file to trigger the initial watcher event.
        var testFile = Path.Combine(_tempDir, "growing.bin");
        await File.WriteAllBytesAsync(testFile, new byte[1024]);

        // Wait for the initial watcher event to fire.
        await Task.Delay(500);
        var countAfterCreate = Volatile.Read(ref eventCount);
        Assert.True(countAfterCreate >= 1, "Expected at least 1 event after file creation");

        // Simulate continuous writes: append data multiple times over several
        // poll intervals so the directory mtime scan detects updated LastWriteTime.
        for (var i = 0; i < 4; i++)
        {
            await Task.Delay(600);
            using var stream = new FileStream(testFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            await stream.WriteAsync(new byte[1024]);
        }

        // Give the last poll tick time to fire.
        await Task.Delay(800);
        var countAfterWrites = Volatile.Read(ref eventCount);

        // We expect more events than just the initial creation — the poll
        // timer should have detected mtime changes and fired additional events.
        Assert.True(countAfterWrites > countAfterCreate,
            $"Expected polling to fire additional events. Before writes: {countAfterCreate}, after: {countAfterWrites}");
    }

    [Fact]
    public async Task PollingStops_WhenFileStopsGrowing()
    {
        _watcher.PollIntervalMs = 500;

        var eventCount = 0;
        _watcher.FileActivityDetected += () => Interlocked.Increment(ref eventCount);

        _watcher.StartMonitoring(_tempDir);
        await Task.Delay(200);

        // Create a file, triggering the watcher and starting the poll timer.
        var testFile = Path.Combine(_tempDir, "static.bin");
        await File.WriteAllBytesAsync(testFile, new byte[1024]);

        // Wait for the initial event(s) plus one poll cycle where
        // no file mtime has changed, causing the poll to stop.
        await Task.Delay(1500);
        var countAfterStabilize = Volatile.Read(ref eventCount);

        // Wait several more poll intervals — no new events should arrive
        // since no files are being modified and the poll timer has stopped.
        await Task.Delay(2000);
        var countAfterWait = Volatile.Read(ref eventCount);

        Assert.Equal(countAfterStabilize, countAfterWait);
    }

    [Fact]
    public async Task NewFileCreated_DetectedByPollFileCount()
    {
        _watcher.PollIntervalMs = 500;

        var eventCount = 0;
        _watcher.FileActivityDetected += () => Interlocked.Increment(ref eventCount);

        _watcher.StartMonitoring(_tempDir);
        await Task.Delay(200);

        // Create an initial file to trigger the watcher and start polling.
        var file1 = Path.Combine(_tempDir, "clip_001.bin");
        await File.WriteAllBytesAsync(file1, new byte[1024]);

        // Wait for watcher event and first poll cycle.
        await Task.Delay(800);
        var countAfterFirst = Volatile.Read(ref eventCount);
        Assert.True(countAfterFirst >= 1, "Expected at least 1 event after first file");

        // Wait for polling to stop (no mtime changes).
        await Task.Delay(1500);
        var countAfterStable = Volatile.Read(ref eventCount);

        // Create a second file — the watcher should pick this up immediately,
        // and polling will also detect the file count change.
        var file2 = Path.Combine(_tempDir, "clip_002.bin");
        await File.WriteAllBytesAsync(file2, new byte[1024]);

        await Task.Delay(800);
        var countAfterSecond = Volatile.Read(ref eventCount);

        Assert.True(countAfterSecond > countAfterStable,
            $"Expected new file creation to fire events. After stable: {countAfterStable}, after second file: {countAfterSecond}");
    }
}
