using MemGuard.Core.Agent;
using MemGuard.Core.Agent.Tools;
using MemGuard.Core.Interfaces;
using Moq;

namespace MemGuard.Tests.Agent;

public class ToolRegistryTests
{
    [Fact]
    public void RegisterTool_Should_Add_Tool_To_Registry()
    {
        // Arrange
        var registry = new ToolRegistry();
        var mockTool = new Mock<AgentTool>();
        mockTool.Setup(t => t.Name).Returns("test_tool");
        mockTool.Setup(t => t.Description).Returns("Test tool");
        mockTool.Setup(t => t.ParametersSchema).Returns("{}");

        // Act
        registry.RegisterTool(mockTool.Object);

        // Assert
        Assert.True(registry.HasTool("test_tool"));
    }

    [Fact]
    public void RegisterTool_Should_Throw_When_Duplicate()
    {
        // Arrange
        var registry = new ToolRegistry();
        var tool1 = new Mock<AgentTool>();
        tool1.Setup(t => t.Name).Returns("test_tool");
        tool1.Setup(t => t.Description).Returns("Test");
        tool1.Setup(t => t.ParametersSchema).Returns("{}");

        var tool2 = new Mock<AgentTool>();
        tool2.Setup(t => t.Name).Returns("test_tool");
        tool2.Setup(t => t.Description).Returns("Test");
        tool2.Setup(t => t.ParametersSchema).Returns("{}");

        registry.RegisterTool(tool1.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => registry.RegisterTool(tool2.Object));
    }

    [Fact]
    public void GetTool_Should_Return_Null_For_Unknown_Tool()
    {
        // Arrange
        var registry = new ToolRegistry();

        // Act
        var result = registry.GetTool("unknown_tool");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllTools_Should_Return_All_Registered_Tools()
    {
        // Arrange
        var registry = new ToolRegistry();
        var tool1 = new Mock<AgentTool>();
        tool1.Setup(t => t.Name).Returns("tool1");
        tool1.Setup(t => t.Description).Returns("Tool 1");
        tool1.Setup(t => t.ParametersSchema).Returns("{}");

        var tool2 = new Mock<AgentTool>();
        tool2.Setup(t => t.Name).Returns("tool2");
        tool2.Setup(t => t.Description).Returns("Tool 2");
        tool2.Setup(t => t.ParametersSchema).Returns("{}");

        registry.RegisterTools(tool1.Object, tool2.Object);

        // Act
        var tools = registry.GetAllTools();

        // Assert
        Assert.Equal(2, tools.Count);
    }
}

public class AgentMemoryTests
{
    [Fact]
    public void AddShortTerm_Should_Add_Memory_Item()
    {
        // Arrange
        var memory = new AgentMemory();

        // Act
        memory.AddShortTerm("user", "test message");

        // Assert
        Assert.Single(memory.ShortTerm);
        Assert.Equal("user", memory.ShortTerm[0].Role);
        Assert.Equal("test message", memory.ShortTerm[0].Content);
    }

    [Fact]
    public void AddFact_Should_Add_To_LongTerm()
    {
        // Arrange
        var memory = new AgentMemory();

        // Act
        memory.AddFact("C# supports async/await");

        // Assert
        Assert.Contains("C# supports async/await", memory.LongTerm);
    }

    [Fact]
    public void AddFact_Should_Not_Add_Duplicates()
    {
        // Arrange
        var memory = new AgentMemory();

        // Act
        memory.AddFact("Fact 1");
        memory.AddFact("Fact 1");

        // Assert
        Assert.Single(memory.LongTerm);
    }

    [Fact]
    public void SetWorking_Should_Store_Key_Value()
    {
        // Arrange
        var memory = new AgentMemory();

        // Act
        memory.SetWorking("file_content", "test content");

        // Assert
        Assert.Equal("test content", memory.GetWorking("file_content"));
    }

    [Fact]
    public void GetSummary_Should_Include_All_Memory_Types()
    {
        // Arrange
        var memory = new AgentMemory();
        memory.AddFact("Test fact");
        memory.SetWorking("key", "value");

        // Act
        var summary = memory.GetSummary();

        // Assert
        Assert.Contains("LEARNED FACTS", summary);
        Assert.Contains("WORKING MEMORY", summary);
    }
}

public class AgentStateTests
{
    [Fact]
    public void GetRecentActions_Should_Return_Last_N_Actions()
    {
        // Arrange
        var state = new AgentState { CurrentTask = "Test" };
        
        for (int i = 0; i < 10; i++)
        {
            state.ExecutedActions.Add(new AgentAction 
            { 
                ToolName = $"tool{i}",
                Reasoning = $"reason{i}",
                Parameters = "{}"
            });
        }

        // Act
        var recent = state.GetRecentActions(3);

        // Assert
        Assert.Equal(3, recent.Count());
        Assert.Equal("tool9", recent.Last().ToolName);
    }

    [Fact]
    public void GetFailedActions_Should_Return_Only_Failed()
    {
        // Arrange
        var state = new AgentState { CurrentTask = "Test" };
        
        var action1 = new AgentAction { ToolName = "tool1", Reasoning = "test", Parameters = "{}" };
        action1.Result = ToolResult.CreateSuccess("tool1", "ok");
        
        var action2 = new AgentAction { ToolName = "tool2", Reasoning = "test", Parameters = "{}" };
        action2.Result = ToolResult.Failure("tool2", "error");
        
        state.ExecutedActions.Add(action1);
        state.ExecutedActions.Add(action2);

        // Act
        var failed = state.GetFailedActions();

        // Assert
        Assert.Single(failed);
        Assert.Equal("tool2", failed.First().ToolName);
    }

    [Fact]
    public void GetSummary_Should_Include_Key_Metrics()
    {
        // Arrange
        var state = new AgentState 
        { 
            CurrentTask = "Test Task",
            IterationCount = 5
        };
        
        state.Errors.Add("Error 1");
        state.Errors.Add("Error 2");

        // Act
        var summary = state.GetSummary();

        // Assert
        Assert.Contains("Test Task", summary);
        Assert.Contains("5", summary);
        Assert.Contains("2", summary);
    }
}
