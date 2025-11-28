using System.Text;
using MemGuard.Core.Agent;

namespace MemGuard.Core.Services;

/// <summary>
/// Builds the context prompt for the AI agent
/// </summary>
public class ContextBuilder
{
    private readonly ToolRegistry _toolRegistry;

    public ContextBuilder(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public string BuildPlanningPrompt(AgentState state, AgentMemory memory)
    {
        var prompt = new StringBuilder();

        // 1. System Role & Identity
        prompt.AppendLine("You are MemGuard AI Agent, an expert .NET developer and autonomous assistant.");
        prompt.AppendLine("Your goal is to complete the assigned task efficiently and correctly.");
        prompt.AppendLine();

        // 2. Available Tools
        prompt.AppendLine("AVAILABLE TOOLS:");
        prompt.AppendLine(_toolRegistry.GetToolsDocumentation());
        prompt.AppendLine();

        // 3. Current Task
        prompt.AppendLine("CURRENT TASK:");
        prompt.AppendLine(state.CurrentTask);
        prompt.AppendLine();

        // 4. Project Context
        if (!string.IsNullOrEmpty(state.ProjectPath))
        {
            prompt.AppendLine($"PROJECT PATH: {state.ProjectPath}");
            prompt.AppendLine();
        }

        // 5. Memory (Long-term & Working)
        var memorySummary = memory.GetSummary();
        if (!string.IsNullOrWhiteSpace(memorySummary))
        {
            prompt.AppendLine("MEMORY CONTEXT:");
            prompt.AppendLine(memorySummary);
        }

        // 6. Execution History (Short-term memory from State)
        if (state.ExecutedActions.Any())
        {
            prompt.AppendLine("EXECUTION HISTORY:");
            foreach (var action in state.GetRecentActions(10)) // Show last 10 actions
            {
                prompt.AppendLine($"Step {state.ExecutedActions.IndexOf(action) + 1}:");
                prompt.AppendLine($"  Action: {action.ToolName}");
                prompt.AppendLine($"  Reasoning: {action.Reasoning}");
                
                var status = action.IsSuccess ? "SUCCESS" : "FAILED";
                prompt.AppendLine($"  Result: {status}");
                
                if (!action.IsSuccess && action.Result != null)
                {
                    prompt.AppendLine($"  Error: {action.Result.Error}");
                }
                else if (action.Result != null && !string.IsNullOrEmpty(action.Result.Output))
                {
                    // Truncate output if too long
                    var output = action.Result.Output;
                    if (output.Length > 500)
                        output = output.Substring(0, 500) + "... (truncated)";
                    
                    prompt.AppendLine($"  Output: {output}");
                }
                prompt.AppendLine();
            }
        }

        // 7. Errors to Avoid
        if (state.Errors.Any())
        {
            prompt.AppendLine("CRITICAL ERRORS TO AVOID (LEARN FROM THESE):");
            foreach (var error in state.Errors.TakeLast(5))
            {
                prompt.AppendLine($"  - {error}");
            }
            prompt.AppendLine();
        }

        // 8. Instructions
        prompt.AppendLine("INSTRUCTIONS:");
        prompt.AppendLine("1. Analyze the history and memory to determine the next step.");
        prompt.AppendLine("2. Select the most appropriate tool.");
        prompt.AppendLine("3. If the user asks a general question or greeting, use 'answer_question' AND then return TASK_COMPLETE in the next step.");
        prompt.AppendLine("4. If you need to read a file, use ReadFile.");
        prompt.AppendLine("5. If you need to run a command, use RunCommand.");
        prompt.AppendLine("6. If the task is complete, return status: TASK_COMPLETE.");
        prompt.AppendLine();

        // 9. Response Format
        prompt.AppendLine("RESPONSE FORMAT (JSON):");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"thought\": \"Detailed reasoning for the next step...\",");
        prompt.AppendLine("  \"tool\": \"tool_name_or_TASK_COMPLETE\",");
        prompt.AppendLine("  \"parameters\": { ... },");
        prompt.AppendLine("  \"expected_outcome\": \"What you expect to achieve...\"");
        prompt.AppendLine("}");
        prompt.AppendLine();
        prompt.AppendLine("For TASK_COMPLETE:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"thought\": \"Task is complete because...\",");
        prompt.AppendLine("  \"status\": \"TASK_COMPLETE\",");
        prompt.AppendLine("  \"summary\": \"Summary of work done...\"");
        prompt.AppendLine("}");

        return prompt.ToString();
    }
}
