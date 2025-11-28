using System.Text;
using System.Text.Json.Serialization;

namespace MemGuard.Core.Agent;

/// <summary>
/// Manages the agent's memory across different scopes
/// </summary>
public class AgentMemory
{
    /// <summary>
    /// Short-term memory: Recent conversation history and actions
    /// </summary>
    public List<MemoryItem> ShortTerm { get; set; } = new();

    /// <summary>
    /// Long-term memory: Learned facts, user preferences, architectural patterns
    /// </summary>
    public List<string> LongTerm { get; set; } = new();

    /// <summary>
    /// Working memory: Active file contents, analysis results, current focus
    /// </summary>
    public Dictionary<string, string> Working { get; set; } = new();

    public void AddShortTerm(string role, string content)
    {
        ShortTerm.Add(new MemoryItem { Role = role, Content = content, Timestamp = DateTime.UtcNow });
    }

    public void AddFact(string fact)
    {
        if (!LongTerm.Contains(fact))
        {
            LongTerm.Add(fact);
        }
    }

    public void SetWorking(string key, string value)
    {
        Working[key] = value;
    }

    public string? GetWorking(string key)
    {
        return Working.TryGetValue(key, out var value) ? value : null;
    }

    public void ClearWorking()
    {
        Working.Clear();
    }

    /// <summary>
    /// Summarize memory for context window
    /// </summary>
    public string GetSummary()
    {
        var sb = new StringBuilder();

        if (LongTerm.Any())
        {
            sb.AppendLine("LEARNED FACTS:");
            foreach (var fact in LongTerm)
            {
                sb.AppendLine($"- {fact}");
            }
            sb.AppendLine();
        }

        if (Working.Any())
        {
            sb.AppendLine("WORKING MEMORY:");
            foreach (var kvp in Working)
            {
                // Truncate long values for summary
                var preview = kvp.Value.Length > 100 ? kvp.Value.Substring(0, 97) + "..." : kvp.Value;
                sb.AppendLine($"- {kvp.Key}: {preview}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public class MemoryItem
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
