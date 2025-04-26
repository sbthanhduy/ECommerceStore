namespace Infrastructure.Helpers;

public class OpenAiSettings
{
    public required string ChatCompletionModelId { get; init; }
    public required string TextEmbeddingModelId { get; init; }
    public required string Endpoint { get; init; }
    public  required string ChatCompletionApiKey { get; init; }
    public required string TextEmbeddingApiKey { get; init; }
}
