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
    public class OrderController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;
        private UserInformation _userInformation = new UserInformation();

        public OrderController(KwiatLuxeDb db)
        {
            _db = db;
        }

        [HttpPost("placeorder")]
        [Authorize]
        public async Task<IActionResult> PlaceOrder()
        {
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            var getUserCart = await _db.Carts.Where(c => c.UserId == userId).Select(c => new { c.CartProducts }).FirstOrDefaultAsync();
            if (getUserCart == null) 
            { 
                return NotFound(new { CartNotFound = $"Cart with UserId {userId} not found." }); 
            }
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 0
            };
            Logger.Log(Severity.DEBUG, $"UserId: {order.UserId}, OrderDate: {order.OrderDate}, TotalAmount: {order.TotalAmount}");
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            decimal totalCost = 0;
            foreach (var product in getUserCart.CartProducts)
            {
                var getProductCost = await _db.Products.FindAsync(product.ProductId);
                if (getProductCost == null)
                {
                    continue;
                }
                totalCost += product.Quantity * getProductCost.ProductPrice;
                var orderProduct = new OrderProduct
                {
                    OrderId = order.Id,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                _db.OrderProducts.Add(orderProduct);
            }
            //test
            Logger.Log(Severity.DEBUG, $"totalCost = {totalCost}");
            order.TotalAmount = totalCost;
            _db.Orders.Update(order);
            //test
            await _db.SaveChangesAsync();
            return Ok(new { OrderId = order.Id, Message = "Order placed successfully." }); //Empty the cart after successful transaction
        }

        [HttpGet("myorders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders() 
        {
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            var myOrders = await _db.Orders.Where(or => or.UserId == userId).Select(or => new
            {
                or.Id,
                or.OrderDate,
                or.OrderProducts,
                or.TotalAmount
            }).ToListAsync();
            return Ok(myOrders);
        }

        [HttpDelete("cancelorder{id}")]
        [Authorize]
        public async Task<IActionResult> CancelOrder([FromRoute] int id) 
        {
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1) return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            var cancelOrder = await _db.Orders.FindAsync(id);
            if (cancelOrder == null) return NotFound(new { OrderNotFound = $"Order with id {id} not found" });
            _db.Orders.Remove(cancelOrder);
            var cancelOrderProducts = await _db.OrderProducts.Where(orp => orp.OrderId == cancelOrder.Id).ToListAsync();
            foreach (var orderProducts in cancelOrderProducts) 
            {
                _db.OrderProducts.Remove(orderProducts);
            }
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
