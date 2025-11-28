using MemGuard.Core.Interfaces;
using MemGuard.Core.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MemGuard.Core.Agent;

/// <summary>
/// Core agent loop engine that implements Plan → Execute → Check → Adjust cycle
/// </summary>
public class AgentLoop
{
    private readonly ILLMClient _ai;
    private readonly ToolRegistry _toolRegistry;
    private readonly ContextBuilder _contextBuilder;

    public AgentLoop(ILLMClient ai, ToolRegistry toolRegistry)
    {
        _ai = ai;
        _toolRegistry = toolRegistry;
        _contextBuilder = new ContextBuilder(toolRegistry);
    }

    /// <summary>
    /// Run the agent loop for a given task
    /// </summary>
    public async Task<AgentState> RunAsync(string task, AgentLoopConfig config, CancellationToken cancellationToken = default)
    {
        var state = new AgentState
        {
            CurrentTask = task,
            ProjectPath = config.ProjectPath
        };

        try
        {
            while (!ShouldStop(state, config))
            {
                state.IterationCount++;
                
                // PLAN: Ask AI to decide next action
                var action = await PlanNextActionAsync(state, cancellationToken);
                
                if (action == null)
                {
                    // AI decided task is complete
                    state.IsComplete = true;
                    state.CompletionReason = "Task completed successfully";
                    break;
                }

                // Notify progress
                config.OnProgress?.Invoke(state, action);

                // EXECUTE: Run the selected tool
                var startTime = DateTime.UtcNow;
                action.Result = await ExecuteActionAsync(action, cancellationToken);
                action.ExecutionDuration = DateTime.UtcNow - startTime;

                state.ExecutedActions.Add(action);

                // CHECK: Verify the result
                if (!action.IsSuccess)
                {
                    var errorMsg = $"Action '{action.ToolName}' failed: {action.Result?.Error}";
                    state.Errors.Add(errorMsg);
                    config.OnError?.Invoke(errorMsg);
                    
                    // INTELLIGENT RETRY DETECTION: Check if we're repeating the same failed action
                    var recentSimilarFailures = state.ExecutedActions
                        .TakeLast(5)
                        .Where(a => a.ToolName == action.ToolName && !a.IsSuccess)
                        .Count();
                    
                    if (recentSimilarFailures >= 3)
                    {
                        var recursionMsg = $"Detected repetitive failures: '{action.ToolName}' failed {recentSimilarFailures} times in a row. Stopping to prevent infinite loop.";
                        state.Errors.Add(recursionMsg);
                        config.OnError?.Invoke(recursionMsg);
                        state.IsComplete = true;
                        state.CompletionReason = "Stopped due to repetitive failures (infinite loop detected)";
                        break;
                    }
                }

                // Notify iteration complete
                config.OnIterationComplete?.Invoke(state);

                // Auto-save checkpoint
                if (config.AutoSaveCheckpoints)
                {
                    var checkpointDir = config.CheckpointDirectory ?? config.ProjectPath ?? Environment.CurrentDirectory;
                    var checkpointManager = new CheckpointManager(checkpointDir);
                    await checkpointManager.SaveCheckpointAsync(state, "autosave");
                }

                // ADJUST: The next iteration will use updated state
                // The AI will see what worked/didn't work and adjust accordingly
            }

            return state;
        }
        catch (Exception ex)
        {
            state.Errors.Add($"Fatal error: {ex.Message}");
            state.IsComplete = true;
            state.CompletionReason = $"Failed: {ex.Message}";
            return state;
        }
    }

    /// <summary>
    /// Plan the next action using AI
    /// </summary>
    private async Task<AgentAction?> PlanNextActionAsync(AgentState state, CancellationToken cancellationToken)
    {
        var prompt = BuildPlanningPrompt(state);
        var response = await _ai.GenerateResponseAsync(prompt, cancellationToken);

        // Parse AI response
        return ParseAIResponse(response);
    }

    /// <summary>
    /// Execute an action using the appropriate tool
    /// </summary>
    private async Task<ToolResult> ExecuteActionAsync(AgentAction action, CancellationToken cancellationToken)
    {
        var tool = _toolRegistry.GetTool(action.ToolName);
        if (tool == null)
        {
            return ToolResult.Failure(action.ToolName, $"Tool '{action.ToolName}' not found");
        }

        return await tool.ExecuteAsync(action.Parameters, cancellationToken);
    }

    /// <summary>
    /// Check if the agent should stop
    /// </summary>
    private bool ShouldStop(AgentState state, AgentLoopConfig config)
    {
        if (state.IsComplete)
            return true;

        if (state.IterationCount >= config.MaxIterations)
        {
            state.IsComplete = true;
            state.CompletionReason = $"Reached maximum iterations ({config.MaxIterations})";
            return true;
        }

        if (state.ElapsedTime >= config.MaxExecutionTime)
        {
            state.IsComplete = true;
            state.CompletionReason = $"Exceeded maximum execution time ({config.MaxExecutionTime})";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Build the planning prompt for the AI
    /// </summary>
    private string BuildPlanningPrompt(AgentState state)
    {
        return _contextBuilder.BuildPlanningPrompt(state, state.Memory);
    }

    /// <summary>
    /// Parse AI response into an action
    /// </summary>
    private AgentAction? ParseAIResponse(string response)
    {
        try
        {
            // Extract JSON from response (AI might include extra text)
            var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}", RegexOptions.Multiline);
            if (!jsonMatch.Success)
            {
                throw new Exception("No JSON found in AI response");
            }

            var json = jsonMatch.Value;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check if task is complete
            if (root.TryGetProperty("status", out var status) && 
                status.GetString() == "TASK_COMPLETE")
            {
                return null; // Signal completion
            }

            // Parse action
            var thought = root.GetProperty("thought").GetString() ?? "";
            var tool = root.GetProperty("tool").GetString() ?? "";
            var parameters = root.GetProperty("parameters").GetRawText();
            var expectedOutcome = root.GetProperty("expected_outcome").GetString() ?? "";

            return new AgentAction
            {
                ToolName = tool,
                Parameters = parameters,
                Reasoning = thought,
                ExpectedOutcome = expectedOutcome
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse AI response: {ex.Message}. Response: {response}");
        }
    }
}
