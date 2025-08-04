using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.FileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController(
        KwiatLuxeDb db,
        IMemoryCache memoryCache,
        Channel<ImageUploadJob> uploadChannel,
        ConcurrentDictionary<string, BackgroundJobStatus> uploadStatus,
        ImageFileService imageFileService)
        : ControllerBase
    {
        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllProducts()
        {
            var productsArray = await db.Products.Select(p => new
            {
                p.Id,
                p.ProductName,
                p.ProductDescription,
                p.ProductPrice,
                p.FileImageUrl
            }).ToListAsync();
            if (productsArray.Count == 0)
            {
                return NotFound(new { ProductNotFound = "No products found." });
            }
            return Ok(productsArray);
        }

        [HttpGet("get/{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            if (!memoryCache.TryGetValue(id, out var cacheProduct))
            {
                var product = await db.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
                }
                cacheProduct = product;
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                };
                memoryCache.Set(id, cacheProduct, cacheEntryOptions);
            }
            return Ok(cacheProduct);
        }

        [HttpGet("imageuploadstatus/{processid}")]
        public IActionResult GetImageUploadStatus(string processid)
        {
            if (!uploadStatus.ContainsKey(processid))
            {
                return BadRequest(new { QueueError = $"Job Id {processid} does not exist." });
            }
            return Ok(new { JobId = processid, Status = uploadStatus[processid].ToString() });
        }

        [HttpGet("productimage/{id}")]
        public async Task<IActionResult> GetProductByIdImage(int id)
        {
            var product = await db.Products.Select(p => new { p.Id, p.FileImageUrl }).Where(p => p.Id == id).FirstOrDefaultAsync();
            if (product == null) return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
            string fullPath = $"{Directory.GetParent(Directory.GetCurrentDirectory())}{Path.DirectorySeparatorChar}Uploads{Path.DirectorySeparatorChar}{product.FileImageUrl}";
            if (System.IO.File.Exists(fullPath))
            {
                StringBuilder sb = new();
                string ext = Path.GetExtension(fullPath);
                if (ext[0] == '.')
                {
                    int count = 0;
                    foreach (char c in ext)
                    {
                        if (c == '.') continue;
                        sb.Append(c);
                        count++;
                    }
                    ext = sb.ToString();
                }
                byte[] imageBytes = System.IO.File.ReadAllBytes(fullPath);
                return File(imageBytes, $"image/{ext}");
            }
            return BadRequest(new { SystemError = $"File Not Found" });
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public async Task<IActionResult> AddProduct([FromForm] ProductDTO? productDto)
        {
            if (productDto == null)
            {
                return BadRequest(new { error = "Product data is NULL." });
            }
            if ((productDto.FileImageUrl != null) && (productDto.FileImageUrl?.Length > 1 * 1024 * 1024))
            {
                return BadRequest(new { FileError = "File size should not exceed 1 MB" });
            }

            if (productDto.FileImageUrl == null)
            {
                var product = new Product
                {
                    ProductName = productDto.ProductName,
                    ProductDescription = productDto.ProductDescription,
                    ProductPrice = productDto.ProductPrice
                };
                db.Products.Add(product);
                await db.SaveChangesAsync();
                return Created($"/add/{product.Id}", product);
            }
            var processId = Guid.NewGuid().ToString();
            var imageUploadJob = new ImageUploadJob
            {
                Id = processId,
                ProductDto = productDto,
                FileUpload = productDto.FileImageUrl,
                Status = BackgroundJobStatus.Queued
            };
            await uploadChannel.Writer.WriteAsync(imageUploadJob);
            uploadStatus[processId] = BackgroundJobStatus.Queued;
            var request = HttpContext.Request;
            return Created($"{request.Scheme}://{request.Host}/Product/imageuploadstatus/{processId}",
                            new
                            {
                                processId = processId,
                                processStatus = BackgroundJobStatus.Queued,
                                queueHelper = "File added to queue"
                            });
        }

        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id)
        {
            //check if product to delete exists
            try
            {
                var deleteProduct = await db.Products.FindAsync(id);
                if (deleteProduct == null) return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
                if (deleteProduct.FileImageUrl != null)
                {
                    imageFileService.DeleteFile(deleteProduct.FileImageUrl);
                }
                db.Remove(deleteProduct);
                await db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(new { error = $"Error: File not found. {e}" });
            }
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromForm] UpdateProductDTO updateProductDto)
        {
            //check if product to update exists
            var updateProduct = await db.Products.FindAsync(id);
            if (updateProduct == null) return NotFound(new { ProductNotFound = $"Product with ID {id} not found." });
            if ((updateProductDto.FileImageUrl != null) && (updateProductDto.FileImageUrl?.Length > 1 * 1024 * 1024)) return BadRequest(new { FileError = "File size should not exceed 1 MB" });
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
                string? createdImageName = null;
                if (updateProduct.FileImageUrl != null)
                {
                    imageFileService.DeleteFile(updateProduct.FileImageUrl);
                    createdImageName = await imageFileService.FileUpload(updateProductDto.FileImageUrl);
                }
                updateProduct.FileImageUrl = createdImageName;
            }
            await db.SaveChangesAsync();
            return Ok(updateProduct);
        }
    }
}
