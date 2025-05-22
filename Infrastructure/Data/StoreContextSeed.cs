using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
#pragma warning disable SKEXP0001

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager,
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        IVectorStoreRecordCollection<string, ProductVector> vectorStoreRecordCollection)
    {
        if (!userManager.Users.Any(x => x.UserName == "admin@test.com"))
        {
            var user = new AppUser
            {
                UserName = "admin@test.com",
                Email = "admin@test.com",
            };

            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (!context.Products.Any())
        {
            var productsData = await File
                .ReadAllTextAsync(path + @"/Data/SeedData/products.json");

            var products = JsonSerializer.Deserialize<List<Product>>(productsData);

            if (products == null) return;

            context.Products.AddRange(products);

            await context.SaveChangesAsync();
            
            await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync();

            var persitedProduct = context.Products.AsAsyncEnumerable();
            
            var productVectors = new List<ProductVector>();
            await foreach (Product p in persitedProduct)
            {
                await Task.Delay(6000);
                var embedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(p.Description);
                productVectors.Add(new ProductVector()
                {
                    Id = p.Id.ToString(),
                    ProductId = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    Type = p.Type,
                    Brand = p.Brand,
                    DescriptionEmbedding = embedding
                });
            }
            if (productVectors.Any())
            {
                await foreach (var key in vectorStoreRecordCollection.UpsertBatchAsync(productVectors))
                {
                    // Process each key as they come in
                    Console.WriteLine($"Upserted item with key: {key}");
                }
            }
            else
            {
                Console.WriteLine("No valid product vectors to upsert.");
            }
           
        }

        
        if (!context.DeliveryMethods.Any())
        {
            var dmData = await File
                .ReadAllTextAsync(path + @"/Data/SeedData/delivery.json");

            var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(dmData);

            if (methods == null) return;

            context.DeliveryMethods.AddRange(methods);

            await context.SaveChangesAsync();
        }

        
    }
}
