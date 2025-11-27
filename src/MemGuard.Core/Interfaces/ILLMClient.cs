namespace MemGuard.Core.Interfaces;

/// <summary>
/// Abstraction for LLM clients (Adapter Pattern)
/// </summary>
public interface ILLMClient
{
    /// <summary>
    /// Generates a response from the LLM
    /// </summary>
    /// <param name="prompt">Prompt to send to the LLM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated response</returns>
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
