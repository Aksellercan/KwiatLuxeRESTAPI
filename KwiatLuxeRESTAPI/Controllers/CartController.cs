using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.Logger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartController(KwiatLuxeDb db) : Controller
    {
        [HttpPost("createcart")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> CreateAndAddToCart([FromBody] CartDTO cartDto)
        {
            if (cartDto.CartProduct == null || cartDto.CartProduct.Count == 0)
            {
                return BadRequest(new { Error = "Cart data is invalid." });
            }
            var userId = UserInformation.GetCurrentUserId(User);
            if (userId == null)
            {
                return NotFound(new { UserNotFound = $"User not found." });
            }
            var identicalUserCart = await db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            if (identicalUserCart != null)
            {
                return BadRequest(new { CartExists = $"Cart with UserId {userId} already exists."});
            }
            Cart cart = new Cart
            {
                UserId = userId.Value,
                TotalAmount = 0
            };
            //get products array and store in a dictionary mapping id to object
            Dictionary<int, Product> productsDictionary = await MapProducts();
            Logger.DEBUG.Log($"UserId: {cart.UserId}, TotalAmount: {cart.TotalAmount}");
            db.Carts.Add(cart);
            await db.SaveChangesAsync();
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
                db.CartProducts.Add(cartProduct);
            }
            Logger.DEBUG.Log($"UserId: {cart.UserId}, TotalAmount: {totalCost}");
            cart.TotalAmount = totalCost;
            db.Update(cart);
            await db.SaveChangesAsync();
            return Ok(new { cart.Id, CartId = cart.UserId, Message = "Cart created successfully." });
        }

        [HttpPost("addcart")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> AddMoreToCart([FromBody] CartDTO cartDto)
        {
            if (cartDto.CartProduct == null || cartDto.CartProduct.Count == 0)
            {
                return BadRequest(new { Error = "Cart data is invalid." });
            }
            var userId = UserInformation.GetCurrentUserId(User);
            if (userId == null)
            {
                return NotFound(new { UserNotFound = $"User not found." });
            }
            var cart = await db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            if (cart == null)
            {
                return NotFound(new { CartNotFound = $"Cart with id {userId} not found." });
            }
            //get products array and store in a dictionary mapping id to object
            Dictionary<int, Product> productsDictionary = await MapProducts();
            decimal newCost = cart.TotalAmount;
            foreach (var product in cartDto.CartProduct)
            {
                Product getProduct = productsDictionary[product.ProductId];
                var cartProductbyId = await db.CartProducts.Where(cp => cp.CartId == cart.Id && cp.ProductId == product.ProductId).FirstOrDefaultAsync();
                if (cartProductbyId != null)
                {
                    Logger.DEBUG.Log($"CartId {cartProductbyId.CartId}, ProductId {cartProductbyId.ProductId}, Quantity {cartProductbyId.Quantity}. Quantity to set {product.Quantity}.");
                    int currentQuantity = cartProductbyId.Quantity;
                    cartProductbyId.Quantity = product.Quantity;
                    db.CartProducts.Update(cartProductbyId);
                    if (getProduct != null)
                    {
                        int targetQuantity = product.Quantity;
                        if (currentQuantity > targetQuantity)
                        {
                            int newQuantity = currentQuantity - targetQuantity;
                            newCost -= (newQuantity * getProduct.ProductPrice);
                            Logger.DEBUG.Log($"if > newCost = {newCost}");
                        }
                        if (currentQuantity < targetQuantity)
                        {
                            int newQuantity = targetQuantity - currentQuantity;
                            newCost += (newQuantity * getProduct.ProductPrice);
                            Logger.DEBUG.Log($"if < newCost = {newCost}");
                        }
                    }
                    continue;
                }
                if (getProduct == null)
                {
                    return NotFound(new { ProductNotFound = $"Product with ID {product.ProductId} not found." });
                }
                newCost += (product.Quantity * getProduct.ProductPrice);
                Logger.DEBUG.Log($"newCost = {newCost}");
                var cartProduct = new CartProduct
                {
                    CartId = cart.Id,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                db.CartProducts.Add(cartProduct);
            }
            Logger.DEBUG.Log($"UserId: {cart.UserId}, TotalAmount: {newCost}");
            cart.TotalAmount = newCost;
            db.Carts.Update(cart);
            await db.SaveChangesAsync();
            return Ok(new { Message = "Added to cart successfully." });
        }

        private async Task<Dictionary<int, Product>> MapProducts()
        {
            var products = await db.Products.ToListAsync();
            Dictionary<int, Product> productsDictionary = new Dictionary<int, Product>();
            foreach (Product product in products)
            {
                productsDictionary.Add(product.Id, product);
            }
            return productsDictionary;
        }

        [HttpGet("mycart")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> GetMyCartItems()
        {
            var userId = UserInformation.GetCurrentUserId(User);
            if (userId == null)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            var myCartItems = await db.Carts.Where(ca => ca.UserId == userId).Select(ca => new
            {
                ca.Id,
                ca.CartProducts,
                ca.TotalAmount
            }).ToListAsync();
            return Ok(myCartItems);
        }

        [HttpDelete("removecart")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> RemoveCart()
        {
            var userId = UserInformation.GetCurrentUserId(User);
            if (userId == null) return NotFound(new { UserNotFound = $"User not found." });
            var removeCart = await db.Carts.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            if (removeCart == null) return NotFound(new { CartNotFound = $"Cart with id {userId} not found" });
            db.Carts.Remove(removeCart);
            var removeAllCartProducts = await db.CartProducts.Where(cp => cp.CartId == removeCart.Id).ToListAsync();
            foreach (var cartProducts in removeAllCartProducts)
            {
                db.CartProducts.Remove(cartProducts);
            }
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
