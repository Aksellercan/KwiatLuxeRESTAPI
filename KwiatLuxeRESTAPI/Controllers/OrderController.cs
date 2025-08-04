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
    public class OrderController(KwiatLuxeDb db) : ControllerBase
    {
        [HttpPost("placeorder")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> PlaceOrder()
        {
            int? userId = UserInformation.GetCurrentUserId(User);
            if (userId == null)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            var getUserCart = await db.Carts.Where(c => c.UserId == userId).Select(c => new { c.CartProducts, c.TotalAmount }).FirstOrDefaultAsync();
            if (getUserCart == null)
            {
                return NotFound(new { CartNotFound = $"Cart with UserId {userId} not found." });
            }

            if (getUserCart.CartProducts == null)
            {
                return BadRequest(new { CartEmpty = $"Cart with UserId {userId} is empty." });
            }

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 0
            };
            Logger.DEBUG.Log($"UserId: {order.UserId}, OrderDate: {order.OrderDate}, TotalAmount: {order.TotalAmount}");
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            foreach (var product in getUserCart.CartProducts)
            {
                var orderProduct = new OrderProduct
                {
                    OrderId = order.Id,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                db.OrderProducts.Add(orderProduct);
            }
            Logger.DEBUG.Log($"totalCost = {getUserCart.TotalAmount}");
            order.TotalAmount = getUserCart.TotalAmount;
            db.Orders.Update(order);
            await db.SaveChangesAsync();
            return Ok(new { OrderId = order.Id, Message = "Order placed successfully." }); //Empty the cart after successful transaction
        }

        [HttpGet("myorders")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> GetMyOrders()
        {
            int? userId = UserInformation.GetCurrentUserId(User);
            if (userId == null)
            {
                return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            }
            var myOrders = await db.Orders.Where(or => or.UserId == userId).Select(or => new
            {
                or.Id,
                or.OrderDate,
                or.OrderProducts,
                or.TotalAmount
            }).ToListAsync();
            return Ok(myOrders);
        }

        [HttpDelete("cancelorder{id}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> CancelOrder([FromRoute] int id)
        {
            int? userId = UserInformation.GetCurrentUserId(User);
            if (userId == null) return NotFound(new { UserNotFound = $"User with id {userId} not found." });
            var cancelOrder = await db.Orders.FindAsync(id);
            if (cancelOrder == null) return NotFound(new { OrderNotFound = $"Order with id {id} not found" });
            db.Orders.Remove(cancelOrder);
            var cancelOrderProducts = await db.OrderProducts.Where(orp => orp.OrderId == cancelOrder.Id).ToListAsync();
            foreach (var orderProducts in cancelOrderProducts)
            {
                db.OrderProducts.Remove(orderProducts);
            }
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
