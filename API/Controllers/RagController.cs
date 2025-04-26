using System.Text.Json;
using API.DTOs;
using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController(IVectorStoreRecordCollection<string, ProductVector> vectorStoreRecordCollection,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    Kernel kernel, 
    VectorStoreTextSearch<ProductVector> vectorStoreTextSearch,
    IChatCompletionService chatCompletionService,
    IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Search(SemanticSearchDto semanticSearchDto, CancellationToken token)
    {
        var query = semanticSearchDto.SearchText;

         KernelSearchResults<object> responese =
            await vectorStoreTextSearch.GetSearchResultsAsync(query, cancellationToken: token);
        
        List<int> ids = new ();
        await foreach (object product in responese.Results.WithCancellation(token))
        {
            ids.Add((product as ProductVector)!.ProductId);
        }
        
       
        var result = await unitOfWork.Repository<Product>().GetOfIdsAsync(ids);
        var count = result.Count;
        var pagination = new Pagination<Product>(1, count, count, result);

        return Ok(pagination);
        
    }

    [Cache(600)]
    [HttpGet("summaries/{id:int}")] 
    public async Task<ActionResult<IReadOnlyList<string>>> GetAiSummary(int id, CancellationToken token)
    {
        var product = await unitOfWork.Repository<Product>().GetByIdAsync(id);
        if (product == null) return Array.Empty<string>();

        var productDescription = product.Description;
        
        var systemPrompt = """
                           You are an AI assistant specialized in analyzing product descriptions from e-commerce websites. Your task is to identify and extract the most important features and requirements of products based on their descriptions.
                           For any given product description:
                           Identify all notable features that would be important to potential buyers (e.g., "waterproof", "shock-resistant", "energy-efficient")
                           Identify all important usage requirements or care instructions (e.g., "hand wash only", "requires assembly", "not suitable for children under 3")
                           Focus on characteristics that differentiate this product from similar ones
                           Include both positive attributes and important limitations/warnings
                           Only extract features that are explicitly stated in the description
                           Return your response as a concise list of strings, formatted exactly like this example:
                           [
                           "Waterproof up to 50 meters",
                           "Dustproof and shock-resistant",
                           "Includes 2-year warranty",
                           "Not suitable for saltwater use",
                           "Requires monthly battery replacement"
                           ]
                           Keep each item brief but descriptive enough to be meaningful (typically 3-8 words). Do not include any additional commentary or explanation. If no notable features are found, return an empty list [].
                           """;
        ChatHistory chatHistory = new();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(productDescription);
        var response = await chatCompletionService.GetChatMessageContentsAsync(chatHistory, kernel:kernel, cancellationToken: token);
        var result = response.FirstOrDefault()?.Content;
        if (string.IsNullOrWhiteSpace(result) || result.Trim() == "[]")
        {
            return Array.Empty<string>();
        }
        var features = JsonSerializer.Deserialize<IReadOnlyList<string>>(result);
        return Ok(features);
    }
}
