namespace MemGuard.Core.Agent;

/// <summary>
/// Represents a single action to be executed by the agent
/// </summary>
public class AgentAction
{
    /// <summary>
    /// Name of the tool to execute
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// Parameters for the tool (JSON format)
    /// </summary>
    public string Parameters { get; init; } = "{}";

    /// <summary>
    /// Agent's reasoning for choosing this action
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// What the agent expects to happen
    /// </summary>
    public string ExpectedOutcome { get; init; } = string.Empty;

    /// <summary>
    /// When this action was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Result of executing this action (set after execution)
    /// </summary>
    public ToolResult? Result { get; set; }

    /// <summary>
    /// Whether this action was successful
    /// </summary>
    public bool IsSuccess => Result?.Success ?? false;

    /// <summary>
    /// How long the action took to execute
    /// </summary>
    public TimeSpan? ExecutionDuration { get; set; }
}

/// <summary>
/// Represents the current state of the agent
/// </summary>
public class AgentState
{
    /// <summary>
    /// The main task the agent is working on
    /// </summary>
    public string CurrentTask { get; set; } = string.Empty;

    /// <summary>
    /// History of all actions executed
    /// </summary>
    public List<AgentAction> ExecutedActions { get; } = new();

    /// <summary>
    /// Errors encountered during execution
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Current iteration number
    /// </summary>
    public int IterationCount { get; set; }

    /// <summary>
    /// Whether the task is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Reason for completion (success message or failure reason)
    /// </summary>
    public string? CompletionReason { get; set; }

    /// <summary>
    /// When the agent started working
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Total time elapsed
    /// </summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;

    /// <summary>
    /// Project context (if applicable)
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Agent's memory system (Short-term, Long-term, Working)
    /// </summary>
    public AgentMemory Memory { get; } = new();

    /// <summary>
    /// Get last N actions
    /// </summary>
    public IEnumerable<AgentAction> GetRecentActions(int count)
    {
        return ExecutedActions.TakeLast(count);
    }

    /// <summary>
    /// Get all failed actions
    /// </summary>
    public IEnumerable<AgentAction> GetFailedActions()
    {
        return ExecutedActions.Where(a => !a.IsSuccess);
    }

    /// <summary>
    /// Get all successful actions
    /// </summary>
    public IEnumerable<AgentAction> GetSuccessfulActions()
    {
        return ExecutedActions.Where(a => a.IsSuccess);
    }

    /// <summary>
    /// Summary of current state for AI context
    /// </summary>
    public string GetSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"Task: {CurrentTask}");
        summary.AppendLine($"Iteration: {IterationCount}");
        summary.AppendLine($"Actions Executed: {ExecutedActions.Count}");
        summary.AppendLine($"Successes: {GetSuccessfulActions().Count()}");
        summary.AppendLine($"Failures: {GetFailedActions().Count()}");
        summary.AppendLine($"Errors: {Errors.Count}");
        summary.AppendLine($"Elapsed: {ElapsedTime:mm\\:ss}");
        
        if (Errors.Any())
        {
            summary.AppendLine("\nRecent Errors:");
            foreach (var error in Errors.TakeLast(3))
            {
                summary.AppendLine($"  - {error}");
            }
        }

        return summary.ToString();
    }
}
