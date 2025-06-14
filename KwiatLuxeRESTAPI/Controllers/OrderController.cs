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
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDTO orderDto)
        {
            if (orderDto == null || orderDto.OrderProduct == null || !orderDto.OrderProduct.Any())
            {
                return BadRequest("Order data is invalid.");
            }
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = orderDto.TotalAmount
            };
            Logger.Log(Severity.DEBUG, $"UserId: {order.UserId}, OrderDate: {order.OrderDate}, TotalAmount: {order.TotalAmount}");
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

        [HttpGet("myorders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders() 
        {
            int userId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId == -1)
            {
                return NotFound($"User with ID {userId} not found.");
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
            if (userId == -1) return NotFound($"User with ID {userId} not found.");
            var cancelOrder = await _db.Orders.FindAsync(id);
            if (cancelOrder == null) return NotFound($"Order with id {id} not found");
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
