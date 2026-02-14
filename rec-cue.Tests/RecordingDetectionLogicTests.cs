using RecCue;
using Xunit;

namespace RecCue.Tests;

public class RecordingDetectionLogicTests : IDisposable
{
    private readonly RecordingDetectionLogic _logic;

    public RecordingDetectionLogicTests()
    {
        _logic = new RecordingDetectionLogic();
    }

    public void Dispose()
    {
        _logic.Dispose();
    }

    [Fact]
    public void InitialState_IsNotRecording()
    {
        Assert.False(_logic.IsRecordingActive);
    }

    [Fact]
    public void OnFileActivityDetected_SetsRecordingActive()
    {
        _logic.OnFileActivityDetected();

        Assert.True(_logic.IsRecordingActive);
    }

    [Fact]
    public void OnFileActivityDetected_FiresRecordingStateChanged()
    {
        var stateChanges = new List<bool>();
        _logic.RecordingStateChanged += state => stateChanges.Add(state);

        _logic.OnFileActivityDetected();

        Assert.Single(stateChanges);
        Assert.True(stateChanges[0]);
    }

    [Fact]
    public void RepeatedActivity_DoesNotFireDuplicateEvents()
    {
        var stateChanges = new List<bool>();
        _logic.RecordingStateChanged += state => stateChanges.Add(state);

        _logic.OnFileActivityDetected();
        _logic.OnFileActivityDetected();
        _logic.OnFileActivityDetected();

        // Only one transition from false -> true
        Assert.Single(stateChanges);
        Assert.True(stateChanges[0]);
    }

    [Fact]
    public async Task InactivityTimeout_SetsRecordingInactive()
    {
        var stateChanges = new List<bool>();
        _logic.RecordingStateChanged += state => stateChanges.Add(state);

        _logic.OnFileActivityDetected();
        Assert.True(_logic.IsRecordingActive);

        // Wait for the 3-second inactivity timeout plus a margin
        await Task.Delay(4000);

        Assert.False(_logic.IsRecordingActive);
        Assert.Equal(2, stateChanges.Count);
        Assert.True(stateChanges[0]);   // active
        Assert.False(stateChanges[1]);  // inactive
    }

    [Fact]
    public async Task RepeatedActivity_ResetsInactivityTimer()
    {
        _logic.OnFileActivityDetected();
        Assert.True(_logic.IsRecordingActive);

        // Wait 2 seconds (within the 3s timeout), then trigger again
        await Task.Delay(2000);
        _logic.OnFileActivityDetected();
        Assert.True(_logic.IsRecordingActive);

        // Wait another 2 seconds -- should still be active because timer was reset
        await Task.Delay(2000);
        Assert.True(_logic.IsRecordingActive);

        // Wait for the full timeout to expire
        await Task.Delay(2000);
        Assert.False(_logic.IsRecordingActive);
    }
}
