using Xunit;
using Moq;
using MemGuard.Infrastructure.Extractors;
using MemGuard.Core;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Tests.Extractors;

public class IocExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ShouldReturnIocDiagnostic()
    {
        // Arrange
        var extractor = new IocExtractor();
        var mockRuntime = new Mock<ClrRuntime>();
        var mockHeap = new Mock<ClrHeap>();
        
        // Setup mock to return empty enumerable (simplified test)
        mockHeap.Setup(h => h.EnumerateObjects()).Returns(Enumerable.Empty<ClrObject>());
        mockRuntime.Setup(r => r.Heap).Returns(mockHeap.Object);

        // Act
        var result = await extractor.ExtractAsync(mockRuntime.Object);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<IocDiagnostic>(result);
        var iocDiag = result as IocDiagnostic;
        Assert.NotNull(iocDiag);
        Assert.Equal(0, iocDiag.ThreatScore); // No threats in empty heap
    }

    [Fact]
    public void Name_ShouldReturnIOC()
    {
        // Arrange & Act
        var extractor = new IocExtractor();

        // Assert
        Assert.Equal("IOC", extractor.Name);
    }

    [Fact]
    public async Task ExtractAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var extractor = new IocExtractor();
        var mockRuntime = new Mock<ClrRuntime>();
        var mockHeap = new Mock<ClrHeap>();
        var cts = new CancellationTokenSource();
        
        mockHeap.Setup(h => h.EnumerateObjects()).Returns(Enumerable.Empty<ClrObject>());
        mockRuntime.Setup(r => r.Heap).Returns(mockHeap.Object);
        
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => extractor.ExtractAsync(mockRuntime.Object, cts.Token));
    }
}
