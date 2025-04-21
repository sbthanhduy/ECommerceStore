using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using CloudinaryDotNet.Actions;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ProductsController(IUnitOfWork unit, IPhotoService photoService) : BaseApiController
{
    [Cache(600)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts(
        [FromQuery]ProductSpecParams specParams)
    {
        var spec = new ProductSpecification(specParams);

        return await CreatePagedResult(unit.Repository<Product>(), spec, specParams.PageIndex, specParams.PageSize);
    }

    [Cache(600)]
    [HttpGet("{id:int}")] // api/products/2
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return NotFound();

        return product;
    }

     [InvalidateCache("api/products|")]
    // [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromForm]ProductDto productDto)
    {
        var product = productDto.ToEntity();

        ImageUploadResult imageUploadResult = await photoService.AddPhotoAsync(productDto.Picture);
        if (imageUploadResult.Error == null)
        {
            product.PictureUrl = imageUploadResult.SecureUrl.ToString();
            product.PicturePublicId = imageUploadResult.PublicId;
        }

        unit.Repository<Product>().Add(product);

        if (await unit.Complete())
        {
            return CreatedAtAction("GetProduct", new { id = product.Id }, new { createProductDto = productDto});
        }

        return BadRequest("Problem creating product");
    }

    [InvalidateCache("api/products|")]
    //[Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, ProductDto productDto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();
        
        product.MapFromDto(productDto);

        if (productDto.Picture.Length > 0) 
        {
            if (!string.IsNullOrEmpty(product.PicturePublicId))
            {
                await photoService.DeletePhotoAsync(product.PicturePublicId);
            }
            ImageUploadResult imageUploadResult = await photoService.AddPhotoAsync(productDto.Picture);
            if (imageUploadResult.Error == null)
            {
                product.PictureUrl = imageUploadResult.SecureUrl.ToString();
                product.PicturePublicId = imageUploadResult.PublicId;
            }
        }
        
        unit.Repository<Product>().Update(product);

        if (await unit.Complete())
        {
            return NoContent();
        }

        return BadRequest("Problem updating the product");
    }

    [InvalidateCache("api/products|")]
    //[Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return NotFound();

        if (!string.IsNullOrEmpty(product.PicturePublicId))
        {
            await photoService.DeletePhotoAsync(product.PicturePublicId);
        }
        
        unit.Repository<Product>().Remove(product);
        
        if (await unit.Complete())
        {
            return NoContent();
        }

        return BadRequest("Problem deleting the product");
    }

    [Cache(10000)]
    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        var spec = new BrandListSpecification();

        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    [Cache(10000)]
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        var spec = new TypeListSpecification();

        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    private bool ProductExists(int id)
    {
        return unit.Repository<Product>().Exists(id);
    }
}
