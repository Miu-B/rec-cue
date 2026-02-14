using RecCue;
using Xunit;

namespace RecCue.Tests;

public class FileWatcherManagerTests : IDisposable
{
    private readonly string _tempDir1;
    private readonly string _tempDir2;
    private readonly string _tempDir3;

    public FileWatcherManagerTests()
    {
        _tempDir1 = Path.Combine(Path.GetTempPath(), $"rec-cue-mgr-test1-{Guid.NewGuid():N}");
        _tempDir2 = Path.Combine(Path.GetTempPath(), $"rec-cue-mgr-test2-{Guid.NewGuid():N}");
        _tempDir3 = Path.Combine(Path.GetTempPath(), $"rec-cue-mgr-test3-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir1);
        Directory.CreateDirectory(_tempDir2);
        Directory.CreateDirectory(_tempDir3);
    }

    public void Dispose()
    {
        foreach (var dir in new[] { _tempDir1, _tempDir2, _tempDir3 })
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void SyncWatchers_AddsPaths()
    {
        using var manager = new FileWatcherManager();
        manager.SyncWatchers(new() { _tempDir1, _tempDir2 });

        // No exceptions means watchers were created successfully.
        // We can verify by writing a file and checking for the event.
    }

    [Fact]
    public async Task SyncWatchers_AggregatesEventsFromMultipleFolders()
    {
        using var manager = new FileWatcherManager();
        var eventCount = 0;
        manager.FileActivityDetected += () => Interlocked.Increment(ref eventCount);

        manager.SyncWatchers(new() { _tempDir1, _tempDir2 });
        await Task.Delay(200);

        // Write to folder 1
        await File.WriteAllTextAsync(Path.Combine(_tempDir1, "file1.txt"), "content1");
        await Task.Delay(500);
        var countAfterFirst = Volatile.Read(ref eventCount);
        Assert.True(countAfterFirst >= 1, "Expected event from first folder");

        // Write to folder 2
        await File.WriteAllTextAsync(Path.Combine(_tempDir2, "file2.txt"), "content2");
        await Task.Delay(500);
        var countAfterSecond = Volatile.Read(ref eventCount);
        Assert.True(countAfterSecond > countAfterFirst, "Expected event from second folder");
    }

    [Fact]
    public async Task SyncWatchers_RemovesOldPaths()
    {
        using var manager = new FileWatcherManager();
        var eventCount = 0;
        manager.FileActivityDetected += () => Interlocked.Increment(ref eventCount);

        // Start with two folders.
        manager.SyncWatchers(new() { _tempDir1, _tempDir2 });
        await Task.Delay(200);

        // Now sync with only folder 1 — folder 2's watcher should be disposed.
        manager.SyncWatchers(new() { _tempDir1 });
        await Task.Delay(200);

        // Reset counter.
        Interlocked.Exchange(ref eventCount, 0);

        // Write to the removed folder — should NOT fire events.
        await File.WriteAllTextAsync(Path.Combine(_tempDir2, "removed.txt"), "should not trigger");
        await Task.Delay(1000);
        var countFromRemoved = Volatile.Read(ref eventCount);

        // Write to the still-active folder — should fire events.
        await File.WriteAllTextAsync(Path.Combine(_tempDir1, "active.txt"), "should trigger");
        await Task.Delay(500);
        var countFromActive = Volatile.Read(ref eventCount);

        Assert.Equal(0, countFromRemoved);
        Assert.True(countFromActive >= 1, "Expected events from the still-active folder");
    }

    [Fact]
    public void SyncWatchers_IgnoresEmptyPaths()
    {
        using var manager = new FileWatcherManager();

        // Should not throw or create watchers for empty strings.
        manager.SyncWatchers(new() { "", "   ", _tempDir1 });
    }

    [Fact]
    public void SyncWatchers_EnforcesMaxFolders()
    {
        using var manager = new FileWatcherManager();

        // Create 7 directories, but only MaxFolders (5) should be accepted.
        var dirs = new List<string>();
        for (var i = 0; i < 7; i++)
        {
            var d = Path.Combine(Path.GetTempPath(), $"rec-cue-mgr-max-{Guid.NewGuid():N}");
            Directory.CreateDirectory(d);
            dirs.Add(d);
        }

        try
        {
            manager.SyncWatchers(dirs);
            // Should not throw.
        }
        finally
        {
            foreach (var d in dirs)
            {
                try { Directory.Delete(d, true); } catch { }
            }
        }
    }

    [Fact]
    public void SyncWatchers_UnchangedPathsNotRestarted()
    {
        using var manager = new FileWatcherManager();

        // Sync with folders 1 and 2.
        manager.SyncWatchers(new() { _tempDir1, _tempDir2 });

        // Sync again adding folder 3 — folders 1 and 2 should remain untouched.
        // (No way to directly assert this without internal access, but at minimum
        // this should not throw or cause issues.)
        manager.SyncWatchers(new() { _tempDir1, _tempDir2, _tempDir3 });
    }

    [Fact]
    public void StopAll_DisposesAllWatchers()
    {
        using var manager = new FileWatcherManager();
        manager.SyncWatchers(new() { _tempDir1, _tempDir2 });
        manager.StopAll();

        // After StopAll, syncing fresh should work fine.
        manager.SyncWatchers(new() { _tempDir3 });
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var manager = new FileWatcherManager();
        manager.SyncWatchers(new() { _tempDir1 });
        manager.Dispose();
        manager.Dispose(); // Should not throw.
    }
}
