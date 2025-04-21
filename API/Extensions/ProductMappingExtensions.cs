using API.DTOs;
using Core.Entities;

namespace API.Extensions;

public static class ProductMappingExtensions
{
    public static Product ToEntity(this ProductDto productDto, string pictureUrl ="", string  picturePublicId ="")
    {
        return new Product()
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            PictureUrl = pictureUrl,
            PicturePublicId = picturePublicId,
            Type = productDto.Type,
            Brand = productDto.Brand,
            QuantityInStock = productDto.QuantityInStock
        };
    }

    public static void MapFromDto(this Product product, ProductDto productDto)
    {
        product.Name = productDto.Name;
        product.Description = productDto.Description;
        product.Price = productDto.Price;
        product.Type = productDto.Type;
        product.Brand = productDto.Brand;
        product.QuantityInStock = productDto.QuantityInStock;
    }

}
