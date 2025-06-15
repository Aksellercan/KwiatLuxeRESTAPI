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
            var identicalUserCart = await _db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            if (identicalUserCart != null) 
            {
                return BadRequest(new { CartExists = $"Cart with UserId {userId} already exists."});
            }
            var cart = new Cart
            {
                UserId = userId,
                TotalAmount = 0
            };
            Logger.Log(Severity.DEBUG, $"UserId: {cart.UserId}, TotalAmount: {cart.TotalAmount}");
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            decimal totalCost = 0;
            foreach (var product in cartDto.CartProduct)
            {
                var getProductCost = await _db.Products.FindAsync(product.ProductId);
                if (getProductCost != null)
                {
                    totalCost += product.Quantity * getProductCost.ProductPrice;
                }
                else { continue; }
                var cartProduct = new CartProduct
                {
                    CartId = cart.Id,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                _db.CartProducts.Add(cartProduct);
            }
            Logger.Log(Severity.DEBUG, $"UserId: {cart.UserId}, TotalAmount: {totalCost}");
            cart.TotalAmount = totalCost;
            _db.Update(cart);
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
            var cart = await _db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            if (cart == null) 
            {
                return NotFound(new { CartNotFound = $"Cart with id {userId} not found." });
            }
            decimal newCost = cart.TotalAmount;
            foreach (var product in cartDto.CartProduct)
            {
                var getProductCost = await _db.Products.FindAsync(product.ProductId);
                var cartProductbyId = await _db.CartProducts.FindAsync(product.ProductId, userId);
                if (cartProductbyId != null)
                {
                    Logger.Log(Severity.DEBUG, $"CartId {cartProductbyId.CartId}, ProductId {cartProductbyId.ProductId}, Quantity {cartProductbyId.Quantity}. Quantity to set {product.Quantity}.");
                    cartProductbyId.Quantity = product.Quantity;
                    _db.CartProducts.Update(cartProductbyId);
                    if (getProductCost != null)
                    {
                        if (cartProductbyId.Quantity > product.Quantity)
                        {
                            newCost -= (product.Quantity * getProductCost.ProductPrice);
                        }
                        else if (cartProductbyId.Quantity < product.Quantity || cartProductbyId.Quantity == 0)
                        {
                            newCost += (product.Quantity * getProductCost.ProductPrice);
                        }
                    }
                    continue;
                }
                if (getProductCost == null) 
                {
                    return NotFound(new { ProductNotFound = $"Product with ID {product.ProductId} not found." });
                }
                newCost += (product.Quantity * getProductCost.ProductPrice);
                var cartProduct = new CartProduct
                {
                    CartId = cart.Id,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                _db.CartProducts.Add(cartProduct);
            }
            Logger.Log(Severity.DEBUG, $"UserId: {cart.UserId}, TotalAmount: {newCost}");
            cart.TotalAmount = newCost;
            _db.Carts.Update(cart);
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

        [HttpDelete("removecart")]
        [Authorize]
        public async Task<IActionResult> RemoveCart()
        {
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1) return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            var removeCart = await _db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            if (removeCart == null) return NotFound(new { OrderNotFound = $"Order with id {userId} not found" });
            _db.Carts.Remove(removeCart);
            var removeAllOrderProducts = await _db.CartProducts.Where(cp => cp.CartId == removeCart.Id).ToListAsync();
            foreach (var cartProducts in removeAllOrderProducts)
            {
                _db.CartProducts.Remove(cartProducts);
            }
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
