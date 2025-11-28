namespace MemGuard.Core.Agent;

/// <summary>
/// Registry for managing available agent tools
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, AgentTool> _tools = new();
    private readonly object _lock = new();

    /// <summary>
    /// Register a tool
    /// </summary>
    public void RegisterTool(AgentTool tool)
    {
        lock (_lock)
        {
            if (_tools.ContainsKey(tool.Name))
            {
                throw new InvalidOperationException($"Tool '{tool.Name}' is already registered");
            }
            _tools[tool.Name] = tool;
        }
    }

    /// <summary>
    /// Register multiple tools
    /// </summary>
    public void RegisterTools(params AgentTool[] tools)
    {
        foreach (var tool in tools)
        {
            RegisterTool(tool);
        }
    }

    /// <summary>
    /// Get a tool by name
    /// </summary>
    public AgentTool? GetTool(string name)
    {
        lock (_lock)
        {
            return _tools.TryGetValue(name, out var tool) ? tool : null;
        }
    }

    /// <summary>
    /// Get all registered tools
    /// </summary>
    public IReadOnlyList<AgentTool> GetAllTools()
    {
        lock (_lock)
        {
            return _tools.Values.ToList();
        }
    }

    /// <summary>
    /// Get tools by category
    /// </summary>
    public IReadOnlyList<AgentTool> GetToolsByCategory(string category)
    {
        lock (_lock)
        {
            return _tools.Values.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>
    /// Check if a tool exists
    /// </summary>
    public bool HasTool(string name)
    {
        lock (_lock)
        {
            return _tools.ContainsKey(name);
        }
    }

    /// <summary>
    /// Get tool documentation for AI prompts
    /// </summary>
    public string GetToolsDocumentation()
    {
        lock (_lock)
        {
            var docs = new System.Text.StringBuilder();
            docs.AppendLine("AVAILABLE TOOLS:");
            docs.AppendLine();

            var categories = _tools.Values.GroupBy(t => t.Category);
            foreach (var category in categories)
            {
                docs.AppendLine($"## {category.Key}");
                foreach (var tool in category)
                {
                    docs.AppendLine($"### {tool.Name}");
                    docs.AppendLine(tool.Description);
                    docs.AppendLine($"Parameters: {tool.ParametersSchema}");
                    docs.AppendLine();
                }
            }

            return docs.ToString();
        }
    }

    /// <summary>
    /// Get tool names as a list (for AI)
    /// </summary>
    public List<string> GetToolNames()
    {
        lock (_lock)
        {
            return _tools.Keys.ToList();
        }
    }
}
