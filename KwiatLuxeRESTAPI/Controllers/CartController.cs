using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KwiatLuxeRESTAPI.Controllers
{
    public class CartController : Controller
    {
        private readonly KwiatLuxeDb _db;

        public CartController(KwiatLuxeDb db) 
        {
            _db = db;
        }

        //[HttpGet("mycart")]
        //[Authorize]
        //public async Task<IActionResult> GetMyCart() 
        //{
            
        //}
    }
}
