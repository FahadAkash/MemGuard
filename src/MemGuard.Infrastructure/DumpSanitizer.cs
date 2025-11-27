using System.Text.RegularExpressions;

namespace MemGuard.Infrastructure;

/// <summary>
/// Sanitizes memory dumps to remove PII
/// </summary>
public class DumpSanitizer
{
    private readonly Regex[] _piiPatterns = {
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"), // Email
        new(@"\b\d{3}-\d{2}-\d{4}\b"), // SSN
        new(@"\b(?:\d{4}[ -]?){3}\d{4}\b"), // Credit card
        new(@"\b\d{3}-\d{3}-\d{4}\b"), // Phone number
        new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b") // IPv4 Address
    };
    
    /// <summary>
    /// Removes PII from dump content
    /// </summary>
    /// <param name="content">Raw dump content</param>
    /// <returns>Sanitized content</returns>
    public string Sanitize(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        foreach (var pattern in _piiPatterns)
        {
            content = pattern.Replace(content, "[REDACTED]");
        }
        return content;
    }
}
