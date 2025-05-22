using System.ClientModel;
using Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

#pragma warning disable SKEXP0001

namespace Infrastructure.Services;

public class TextEmbeddingGenerationService(): ITextEmbeddingGenerationService
{

    private readonly EmbeddingClient _client;
    public TextEmbeddingGenerationService(IOptions<OpenAiSettings> apiOptions) : this()
    {
        
        var options = new OpenAIClientOptions()
        {
            Endpoint = new Uri(apiOptions.Value.Endpoint),
        };

        _client = new EmbeddingClient(apiOptions.Value.TextEmbeddingModelId,
            new ApiKeyCredential(apiOptions.Value.TextEmbeddingApiKey), options);
    }
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        OpenAIEmbeddingCollection response = _client.GenerateEmbeddings(data, cancellationToken: cancellationToken);
        var result = response.Select(embedding => embedding.ToFloats()).ToList();
        return Task.FromResult<IList<ReadOnlyMemory<float>>>(result);
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; }
}
