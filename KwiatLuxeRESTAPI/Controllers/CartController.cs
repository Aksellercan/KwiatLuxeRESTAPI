using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.Logger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartController : Controller
    {
        private readonly KwiatLuxeDb _db;
        private UserInformation _userInformation = new UserInformation();

        public CartController(KwiatLuxeDb db) 
        {
            _db = db;
        }

        [HttpPost("createcart")]
        [Authorize]
        public async Task<IActionResult> CreateAndAddToCart([FromBody] CartDTO cartDto)
        {
            if (cartDto == null || cartDto.CartProduct == null || !cartDto.CartProduct.Any())
            {
                return BadRequest("Cart data is invalid.");
            }
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound($"User with id {userId} not found.");
            }
            var cart = new Cart
            {
                UserId = userId,
                TotalAmount = cartDto.TotalAmount
            };
            Logger.Log(Severity.DEBUG, $"UserId: {cart.UserId}, TotalAmount: {cart.TotalAmount}");
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            foreach (var product in cartDto.CartProduct)
            {
                var cartProduct = new CartProduct
                {
                    CartId = cart.UserId,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                _db.CartProducts.Add(cartProduct);
            }
            await _db.SaveChangesAsync();
            return Ok(new { CartId = cart.UserId, Message = "Cart created successfully." });
        }

        [HttpPost("addcart")]
        [Authorize]
        public async Task<IActionResult> AddMoreToCart([FromBody] CartDTO cartDto)
        {
            if (cartDto == null || cartDto.CartProduct == null || !cartDto.CartProduct.Any())
            {
                return BadRequest("Cart data is invalid.");
            }
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound($"User with id {userId} not found.");
            }
            foreach (var product in cartDto.CartProduct)
            {
                var cartProduct = new CartProduct
                {
                    CartId = userId,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                _db.CartProducts.Add(cartProduct);
            }
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Added to cart successfully." });
        }

        [HttpGet("mycart")]
        [Authorize]
        public async Task<IActionResult> GetMyCartItems()
        {
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound($"User with id {userId} not found.");
            }
            var myCartItems = await _db.Carts.Where(ca => ca.UserId == userId).Select(ca => new
            {
                ca.Id,
                ca.CartProducts,
                ca.TotalAmount
            }).ToListAsync();
            return Ok(myCartItems);
        }
    }
}
