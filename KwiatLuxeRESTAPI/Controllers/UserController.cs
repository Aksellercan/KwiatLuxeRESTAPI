using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.Logger;
using KwiatLuxeRESTAPI.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;
        private Password _passwordService = new Password();
        private UserInformation _userInformation = new UserInformation();

        public UserController(KwiatLuxeDb db)
        {
            _db = db;
        }

        [Authorize]
        [HttpDelete("removeuser")]
        public async Task<IActionResult> removeUser()
        {
            int currentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var removeUser = await _db.Users.FindAsync(currentUserId);
            if (removeUser == null)
            {
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            }
            try
            {
                _db.Users.Remove(removeUser);
                Logger.Log(Severity.DEBUG, $"User with id {removeUser.Id} is successfully removed");
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new { Error = $"Failed to remove user with id {currentUserId}. {e.Message}" });
            }
            return NoContent();
        }

        [Authorize]
        [HttpPut("updateusername")]
        public async Task<IActionResult> updateUsername(string newUsername)
        {
            int currentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var updateUsername = await _db.Users.FindAsync(currentUserId);
            if (updateUsername == null) 
            { 
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." }); 
            }
            try
            {
                updateUsername.Username = newUsername;
                Logger.Log(Severity.DEBUG, $"Username updated to {updateUsername.Username}");
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new { Error = $"Failed to update username for user with id {currentUserId}. {e.Message}" });
            }
            return Ok(new { Message = "Successfully updated Username"});
        }

        [Authorize]
        [HttpPut("updatemail")]
        public async Task<IActionResult> updateUsermail(string newMail)
        {
            int currentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var changeMail = await _db.Users.FindAsync(currentUserId);
            if (changeMail == null)
            {
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            }
            try
            {
                changeMail.Email = newMail;
                Logger.Log(Severity.DEBUG, $"Email updated to {changeMail.Email}");
                await _db.SaveChangesAsync();
            }
            catch (Exception e) 
            {
                return BadRequest(new { Error = $"Failed to update email for user with id {currentUserId}. {e.Message}" });
            }
            return Ok(new { Message = "Successfully updated Email" });
        }

        [Authorize]
        [HttpPut("updatepassword")]
        public async Task<IActionResult> updateUserPassword(string newPassword)
        {
            int currentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var changePassword = await _db.Users.FindAsync(currentUserId);
            if (changePassword == null)
            {
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            }

            try
            {
                byte[] compareSalt = Convert.FromBase64String(changePassword.Salt);
                string compareHashes = _passwordService.HashPassword(newPassword, compareSalt);
                if (string.Equals(changePassword.Password, compareHashes))
                {
                    Logger.Log(Severity.DEBUG, "New Password is same as old one");
                    return BadRequest(new { Error = "New Password is same as old one" });
                }
                byte[] newSalt = _passwordService.createSalt(256);
                string saltBase64tring = Convert.ToBase64String(newSalt);
                string newHashedPassword = _passwordService.HashPassword(newPassword, newSalt);
                changePassword.Password = newHashedPassword;
                changePassword.Salt = saltBase64tring;
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new { Error = $"Failed to update password for user with id {currentUserId}. {e.Message}" });
            }
            return Ok(new { Message = "Successfully updated User Password" });
        }
    }
}
