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
            //get products array and store in a dictionary mapping id to object
            var products = await _db.Products.ToListAsync();
            Dictionary<int, Product> productsDictionary = MapProducts(products);
            Logger.Log(Severity.DEBUG, $"UserId: {cart.UserId}, TotalAmount: {cart.TotalAmount}");
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            decimal totalCost = 0;
            foreach (var product in cartDto.CartProduct)
            {
                Product getProduct = productsDictionary[product.ProductId];
                if (getProduct != null)
                {
                    totalCost += product.Quantity * getProduct.ProductPrice;
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
            //get products array and store in a dictionary mapping id to object
            var products = await _db.Products.ToListAsync();
            Dictionary<int, Product> productsDictionary = MapProducts(products);
            decimal newCost = cart.TotalAmount;
            foreach (var product in cartDto.CartProduct)
            {
                Product getProduct = productsDictionary[product.ProductId];
                var cartProductbyId = await _db.CartProducts.Where(cp => cp.CartId == cart.Id && cp.ProductId == product.ProductId).FirstOrDefaultAsync();
                if (cartProductbyId != null)
                {
                    Logger.Log(Severity.DEBUG, $"CartId {cartProductbyId.CartId}, ProductId {cartProductbyId.ProductId}, Quantity {cartProductbyId.Quantity}. Quantity to set {product.Quantity}.");
                    int currentQuantity = cartProductbyId.Quantity;
                    cartProductbyId.Quantity = product.Quantity;
                    _db.CartProducts.Update(cartProductbyId);
                    if (getProduct != null)
                    {
                        int targetQuantity = product.Quantity;
                        if (currentQuantity > targetQuantity)
                        {
                            int newQuantity = currentQuantity - targetQuantity;
                            newCost -= (newQuantity * getProduct.ProductPrice);
                            Logger.Log(Severity.DEBUG, $"if > newCost = {newCost}");
                        }
                        if (currentQuantity < targetQuantity)
                        {
                            int newQuantity = targetQuantity - currentQuantity;
                            newCost += (newQuantity * getProduct.ProductPrice);
                            Logger.Log(Severity.DEBUG, $"if < newCost = {newCost}");
                        }
                    }
                    continue;
                }
                if (getProduct == null) 
                {
                    return NotFound(new { ProductNotFound = $"Product with ID {product.ProductId} not found." });
                }
                newCost += (product.Quantity * getProduct.ProductPrice);
                Logger.Log(Severity.DEBUG, $"newCost = {newCost}");
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

        private Dictionary<int, Product> MapProducts(List<Product> products) 
        {
            Dictionary<int, Product> productsDictionary = new Dictionary<int, Product>();
            for (int i = 0; i < products.Count; i++) 
            {
                productsDictionary.Add(products[i].Id, products[i]);
            }
            return productsDictionary;
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
            if (removeCart == null) return NotFound(new { CartNotFound = $"Cart with id {userId} not found" });
            _db.Carts.Remove(removeCart);
            var removeAllCartProducts = await _db.CartProducts.Where(cp => cp.CartId == removeCart.Id).ToListAsync();
            foreach (var cartProducts in removeAllCartProducts)
            {
                _db.CartProducts.Remove(cartProducts);
            }
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
