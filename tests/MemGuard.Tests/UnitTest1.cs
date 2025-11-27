using MemGuard.Core.Services;
using MemGuard.Core.Interfaces;

namespace MemGuard.Tests;

public class MemleaksDetectorServiceTests
{
    [Fact]
    public void Track_Should_Add_Object_To_TrackedObjects()
    {
        // Arrange
        var detector = new MemleaksDetectorService();
        var testObject = new object();
        var key = "testKey";

        // Act
        detector.Track(key, testObject);

        // Assert
        var result = detector.DetectMemleaks();
        Assert.Contains(key, result);
    }

    [Fact]
    public void Track_With_Null_Or_Empty_Key_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var detector = new MemleaksDetectorService();
        var testObject = new object();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => detector.Track(null!, testObject));
        Assert.Throws<ArgumentNullException>(() => detector.Track("", testObject));
        Assert.Throws<ArgumentNullException>(() => detector.Track("   ", testObject));
    }

    [Fact]
    public void UnTrack_Should_Remove_Object_From_TrackedObjects()
    {
        // Arrange
        var detector = new MemleaksDetectorService();
        var testObject = new object();
        var key = "testKey";
        detector.Track(key, testObject);

        // Act
        detector.UnTrack(key);

        // Assert
        var result = detector.DetectMemleaks();
        Assert.DoesNotContain(key, result);
    }

    [Fact]
    public void UnTrack_With_Null_Or_Empty_Key_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var detector = new MemleaksDetectorService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => detector.UnTrack(null!));
        Assert.Throws<ArgumentNullException>(() => detector.UnTrack(""));
        Assert.Throws<ArgumentNullException>(() => detector.UnTrack("   "));
    }

    [Fact]
    public void Reset_Should_Clear_All_Tracked_Objects()
    {
        // Arrange
        var detector = new MemleaksDetectorService();
        var testObject1 = new object();
        var testObject2 = new object();
        detector.Track("key1", testObject1);
        detector.Track("key2", testObject2);

        // Act
        detector.Reset();

        // Assert
        var result = detector.DetectMemleaks();
        Assert.Empty(result);
    }

    [Fact]
    public void DetectMemleaks_Should_Return_Empty_List_When_No_Objects_Tracked()
    {
        // Arrange
        var detector = new MemleaksDetectorService();

        // Act
        var result = detector.DetectMemleaks();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void DetectMemleaks_Should_Return_Objects_That_Are_Still_Alive()
    {
        // Arrange
        var detector = new MemleaksDetectorService();
        var testObject = new object();
        var key = "testKey";
        detector.Track(key, testObject);

        // Act
        var result = detector.DetectMemleaks();

        // Assert
        Assert.Contains(key, result);
        Assert.Single(result);
    }

    [Fact]
    public void DetectMemleaks_Should_Return_Empty_List_When_Objects_Are_Garbage_Collected()
    {
        // Arrange
        var detector = new MemleaksDetectorService();
        var key = "testKey";
        
        // Create object in separate method to ensure it can be garbage collected
        TrackTemporaryObject(detector, key);

        // Act
        var result = detector.DetectMemleaks();

        // Assert
        Assert.DoesNotContain(key, result);
    }

    private static void TrackTemporaryObject(IMemleaksDetector detector, string key)
    {
        var testObject = new object();
        detector.Track(key, testObject);
        // testObject goes out of scope here and should be eligible for garbage collection
    }
}