namespace MemGuard.AI.Interface;

/// <summary>
/// Abstraction for LLM clients
/// </summary>
 
public interface ILLMClient
{
     /// <summary>
    /// Generates a response from the LLM
    /// </summary>
    /// <param name="prompt">Prompt to send to the LLM</param>
    /// <returns>Generated response</returns>
    Task<string> GenerateResponseAsync(string prompt);
}

