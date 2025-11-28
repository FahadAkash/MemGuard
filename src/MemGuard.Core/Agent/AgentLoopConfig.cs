namespace MemGuard.Core.Agent;

/// <summary>
/// Configuration for the agent loop
/// </summary>
public class AgentLoopConfig
{
    /// <summary>
    /// Maximum number of iterations before stopping
    /// </summary>
    public int MaxIterations { get; set; } = 50;

    /// <summary>
    /// Maximum execution time before stopping
    /// </summary>
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Whether to enable verbose logging
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Whether to require user confirmation before executing tools
    /// </summary>
    public bool RequireConfirmation { get; set; } = false;

    /// <summary>
    /// Project path (if working on a project)
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Whether to auto-save checkpoints after each iteration
    /// </summary>
    public bool AutoSaveCheckpoints { get; set; } = true;

    /// <summary>
    /// Directory to save checkpoints (defaults to .memguard/checkpoints in ProjectPath)
    /// </summary>
    public string? CheckpointDirectory { get; set; }

    /// <summary>
    /// Callback for progress updates
    /// </summary>
    public Action<AgentState, AgentAction>? OnProgress { get; set; }

    /// <summary>
    /// Callback for iteration completion
    /// </summary>
    public Action<AgentState>? OnIterationComplete { get; set; }

    /// <summary>
    /// Callback for errors
    /// </summary>
    public Action<string>? OnError { get; set; }
}
