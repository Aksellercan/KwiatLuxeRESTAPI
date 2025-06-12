using Microsoft.AspNetCore.Mvc;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;

        public ProductController(KwiatLuxeDb db)
        {
            _db = db;
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _db.Products.Select(p => new {
                p.Id,
                p.ProductName,
                p.ProductDescription,
                p.ProductPrice,
                p.FileImageUrl
            }).ToListAsync();
            if (products == null || !products.Any())
            {
                return NotFound("No products found.");
            }
            return Ok(products);
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound($"Product with ID {id} not found.");
            }
            return Ok(product);
        }

        [HttpPost("add")]
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
        public async Task<IActionResult> DeleteProduct([FromRoute] int id) 
        {
            //check if product to delete exists
            var deleteProduct = await _db.Products.FindAsync(id);
            if (deleteProduct == null) return NotFound($"Product with id {id} does not exist.");
            _db.Remove(deleteProduct);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] ProductDTO productDTO)
        {
            //check if product to update exists
            var updateProduct = await _db.Products.FindAsync(id);
            if (updateProduct == null) return NotFound($"Product with id {id} does not exist.");
            updateProduct.ProductName = productDTO.ProductName;
            if (productDTO.ProductDescription != null)
            {
                updateProduct.ProductDescription = productDTO.ProductDescription;
            }
            updateProduct.ProductPrice = productDTO.ProductPrice;
            if (productDTO.FileImageUrl != null)
            {
                updateProduct.FileImageUrl = productDTO.FileImageUrl;
            }
            await _db.SaveChangesAsync();
            return Ok(updateProduct);
        }
    }
}
