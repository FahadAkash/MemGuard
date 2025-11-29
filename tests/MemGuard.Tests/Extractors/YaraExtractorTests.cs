using Xunit;
using MemGuard.Infrastructure.Extractors;
using Moq;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Tests.Extractors;

public class YaraExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ShouldReturnYaraDiagnostic()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var extractor = new YaraExtractor(tempDir);
        var mockRuntime = new Mock<ClrRuntime>();

        try
        {
            // Act
            var result = await extractor.ExtractAsync(mockRuntime.Object);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Core.YaraDiagnostic>(result);
            var yaraDiag = result as Core.YaraDiagnostic;
            Assert.NotNull(yaraDiag);
            Assert.Empty(yaraDiag.Matches); // No matches without YARA library
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Name_ShouldReturnYARA()
    {
        // Arrange & Act
        var extractor = new YaraExtractor();

        // Assert
        Assert.Equal("YARA", extractor.Name);
    }

    [Fact]
    public async Task ExtractAsync_ShouldCreateDefaultRulesDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var extractor = new YaraExtractor(tempDir);
        var mockRuntime = new Mock<ClrRuntime>();

        try
        {
            // Act
            await extractor.ExtractAsync(mockRuntime.Object);

            // Assert
            Assert.True(Directory.Exists(tempDir));
            
            // Check for default rules file
            var defaultRules = Path.Combine(tempDir, "default_rules.yar");
            Assert.True(File.Exists(defaultRules));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
