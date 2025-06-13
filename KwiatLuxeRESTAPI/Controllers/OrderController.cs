using Microsoft.AspNetCore.Mvc;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.DTOs;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;

        public OrderController(KwiatLuxeDb db)
        {
            _db = db;
        }

        [HttpPost("order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDTO orderDto)
        {
            if (orderDto == null || orderDto.OrderProduct == null || !orderDto.OrderProduct.Any())
            {
                return BadRequest("Order data is invalid.");
            }
            var user = await _db.Users.FindAsync(orderDto.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {orderDto.UserId} not found.");
            }
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                TotalAmount = orderDto.TotalAmount
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            foreach (var product in orderDto.OrderProduct)
            {
                var orderProduct = new OrderProduct
                {
                    OrderId = order.Id,
                    ProductId = product.ProductId,
                    Quantity = product.Quantity
                };
                _db.OrderProducts.Add(orderProduct);
            }
            await _db.SaveChangesAsync();
            return Ok(new { OrderId = order.Id, Message = "Order placed successfully." });
        }
    }
}
