using System.Text.Json;

namespace MemGuard.Core.Agent;

/// <summary>
/// Base class for all agent tools
/// </summary>
public abstract class AgentTool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string ParametersSchema { get; }
    public virtual string Category => "General";

    public async Task<ToolResult> ExecuteAsync(string parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = ValidateParameters(parameters);
            if (!validationResult.IsValid)
            {
                return ToolResult.Failure(Name, $"Invalid parameters: {validationResult.Error}");
            }

            var result = await ExecuteInternalAsync(parameters, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(Name, $"Tool execution failed: {ex.Message}", ex);
        }
    }

    protected abstract Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken);

    protected virtual ValidationResult ValidateParameters(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return ValidationResult.Invalid("Parameters cannot be empty");
        }

        try
        {
            JsonDocument.Parse(parameters);
            return ValidationResult.Valid();
        }
        catch (JsonException ex)
        {
            return ValidationResult.Invalid($"Invalid JSON: {ex.Message}");
        }
    }

    protected T? DeserializeParameters<T>(string parameters)
    {
        return JsonSerializer.Deserialize<T>(parameters, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
    }
}

public class ToolResult
{
    public bool Success { get; init; }
    public string ToolName { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public string? Error { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();

    public static ToolResult CreateSuccess(string toolName, string output, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            Success = true,
            ToolName = toolName,
            Output = output,
            Metadata = metadata ?? new()
        };
    }

    public static ToolResult Failure(string toolName, string error, Exception? exception = null)
    {
        return new ToolResult
        {
            Success = false,
            ToolName = toolName,
            Error = error,
            Exception = exception,
            Output = string.Empty
        };
    }
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }

    public static ValidationResult Valid() => new() { IsValid = true };
    public static ValidationResult Invalid(string error) => new() { IsValid = false, Error = error };
}
