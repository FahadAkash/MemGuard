using MemGuard.Infrastructure;
using Xunit;

namespace MemGuard.Tests;

public class DumpSanitizerTests
{
    private readonly DumpSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_RemovesEmailAddresses()
    {
        var input = "Contact user@example.com for more info.";
        var expected = "Contact [REDACTED] for more info.";
        Assert.Equal(expected, _sanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_RemovesSSN()
    {
        var input = "SSN is 123-45-6789.";
        var expected = "SSN is [REDACTED].";
        Assert.Equal(expected, _sanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_RemovesCreditCard()
    {
        var input = "Card: 1234-5678-9012-3456";
        var expected = "Card: [REDACTED]";
        Assert.Equal(expected, _sanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_RemovesIPv4()
    {
        var input = "Server IP: 192.168.1.1";
        var expected = "Server IP: [REDACTED]";
        Assert.Equal(expected, _sanitizer.Sanitize(input));
    }
}
