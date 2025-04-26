using Microsoft.Extensions.VectorData;

namespace Core.Entities;

public class ProductVector 
{
    [VectorStoreRecordKey]
    public string Id { get; set; }
    
    [VectorStoreRecordData(IsFilterable = true)]
    public required int ProductId { get; set; }
    
    [VectorStoreRecordData(IsFilterable = true)]
    public required string Name { get; set; }
    
    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public required string Description { get; set; }
    
    [VectorStoreRecordData(IsFilterable = true)]
    public decimal Price { get; set; }
    
    [VectorStoreRecordData(IsFilterable = true)]
    public required string Type { get; set; }
    
    [VectorStoreRecordData(IsFilterable = true)]
    public required string Brand { get; set; }
    
    [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

}
