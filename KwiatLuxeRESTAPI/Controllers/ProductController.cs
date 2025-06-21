using Microsoft.AspNetCore.Mvc;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using KwiatLuxeRESTAPI.Services.Logger;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;
        private readonly IMemoryCache _memoryCache;

        public ProductController(KwiatLuxeDb db, IMemoryCache memoryCache)
        {
            _db = db;
            _memoryCache = memoryCache;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllProducts()
        {
            var productsArray = await _db.Products.Select(p => new {
                p.Id,
                p.ProductName,
                p.ProductDescription,
                p.ProductPrice,
                p.FileImageUrl
            }).ToListAsync();
            if (productsArray == null || !productsArray.Any())
            {
                return NotFound(new { ProductNotFound = "No products found." });
            }
            return Ok(productsArray);
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            Product cacheProduct;
            if (!_memoryCache.TryGetValue(id, out cacheProduct))
            {
                var product = await _db.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
                }
                cacheProduct = product;
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                };
                _memoryCache.Set(id, cacheProduct, cacheEntryOptions);
            }
            return Ok(cacheProduct);
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public async Task<IActionResult> AddProduct([FromBody] ProductDTO productDto)
        {
            if (productDto == null)
            {
                return BadRequest("Product data is null.");
            }
            var product = new Product
            {
                ProductName = productDto.ProductName,
                ProductDescription = productDto.ProductDescription,
                ProductPrice = productDto.ProductPrice,
                FileImageUrl = productDto.FileImageUrl
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return Created($"/add/{product.Id}", product);
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id) 
        {
            //check if product to delete exists
            var deleteProduct = await _db.Products.FindAsync(id);
            if (deleteProduct == null) return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
            _db.Remove(deleteProduct);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] UpdateProductDTO updateProductDto)
        {
            //check if product to update exists
            var updateProduct = await _db.Products.FindAsync(id);
            if (updateProduct == null) return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
            if (updateProductDto.ProductName != null) 
            {
                updateProduct.ProductName = updateProductDto.ProductName;
            }
            if (updateProductDto.ProductDescription != null)
            {
                updateProduct.ProductDescription = updateProductDto.ProductDescription;
            }
            if (updateProductDto.ProductPrice != null)
            {
                updateProduct.ProductPrice = (decimal)updateProductDto.ProductPrice;
            }
            if (updateProductDto.FileImageUrl != null)
            {
                updateProduct.FileImageUrl = updateProductDto.FileImageUrl;
            }
            await _db.SaveChangesAsync();
            return Ok(updateProduct);
        }
    }
}
