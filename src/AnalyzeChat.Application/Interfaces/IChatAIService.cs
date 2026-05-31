namespace AnalyzeChat.Application.Interfaces;

/// <summary>
/// Communicates with the AI model (ChatGPT) for generating responses.
/// </summary>
public interface IChatAIService
{
    /// <summary>
    /// Streams a response from the AI model given a prompt, yielding text chunks.
    /// </summary>
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a response from the AI model given a prompt, yielding text chunks.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(string prompt, string model, CancellationToken cancellationToken = default);
}
