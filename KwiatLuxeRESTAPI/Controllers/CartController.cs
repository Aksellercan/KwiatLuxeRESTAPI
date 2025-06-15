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
                return BadRequest(new { Error = "Cart data is invalid." });
            }
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            var identicalUserCart = await _db.Carts.FindAsync(userId);
            if (identicalUserCart != null) 
            {
                return BadRequest(new { CartExists = $"Cart with UserId {userId} already exists."});
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
            return Ok(new { cart.Id, CartId = cart.UserId, Message = "Cart created successfully." });
        }

        [HttpPost("addcart")]
        [Authorize]
        public async Task<IActionResult> AddMoreToCart([FromBody] CartDTO cartDto)
        {
            if (cartDto == null || cartDto.CartProduct == null || !cartDto.CartProduct.Any())
            {
                return BadRequest(new { Error = "Cart data is invalid." });
            }
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            foreach (var product in cartDto.CartProduct)
            {
                var cartProductbyId = await _db.CartProducts.FindAsync(product.ProductId, userId);
                if (cartProductbyId != null)
                {
                    Logger.Log(Severity.DEBUG, $"CartId {cartProductbyId.CartId}, ProductId {cartProductbyId.ProductId}, Quantity {cartProductbyId.Quantity}. " +
                        $"Quantity to set {product.Quantity}.");
                    cartProductbyId.Quantity = product.Quantity;
                    _db.CartProducts.Update(cartProductbyId);
                    continue;
                }
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
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
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
