using System.Threading.Tasks;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for answering user questions without performing actions
/// </summary>
public class AnswerQuestionTool : AgentTool
{
    public override string Name => "answer_question";
    public override string Description => "Answer a general question from the user, explain capabilities, or provide information without modifying the system.";
    public override string Category => "Communication";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""answer"": {
      ""type"": ""string"",
      ""description"": ""The text response to show to the user""
    }
  },
  ""required"": [""answer""]
}";

    protected override Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<AnswerArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Answer))
        {
            return Task.FromResult(ToolResult.Failure(Name, "Answer parameter is required"));
        }

        // The answer will be displayed by the UI
        return Task.FromResult(ToolResult.CreateSuccess(Name, args.Answer));
    }

    private class AnswerArgs
    {
        public string Answer { get; set; } = string.Empty;
    }
}
