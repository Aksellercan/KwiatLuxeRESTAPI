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
            var ProductDTO = new ProductDTO
            {
                Id = product.Id,
                ProductName = product.ProductName,
                ProductDescription = product.ProductDescription,
                ProductPrice = product.ProductPrice,
                FileImageUrl = product.FileImageUrl
            };
            return Ok(ProductDTO);
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
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
    }
}
