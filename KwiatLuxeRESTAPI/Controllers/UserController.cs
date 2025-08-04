using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.Logger;
using KwiatLuxeRESTAPI.Services.Security.Password;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController(KwiatLuxeDb db) : ControllerBase
    {
        private readonly Password _passwordService = new Password();

        [Authorize(Policy = "AccessToken")]
        [HttpDelete("removeuser")]
        public async Task<IActionResult> RemoveUser()
        {
            var currentUserId = UserInformation.GetCurrentUserId(User);
            if (currentUserId == -1) return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            var removeUser = await db.Users.FindAsync(currentUserId);
            if (removeUser == null)
            {
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            }

            try
            {
                db.Users.Remove(removeUser);
                Logger.DEBUG.Log($"User with id {removeUser.Id} is successfully removed");
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new { Error = $"Failed to remove user with id {currentUserId}. {e.Message}" });
            }

            return NoContent();
        }

        [Authorize(Policy = "AccessToken")]
        [HttpPut("updateusername")]
        public async Task<IActionResult> UpdateUsername(string newUsername)
        {
            var currentUserId = UserInformation.GetCurrentUserId(User);
            if (currentUserId == -1) return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            var updateUsername = await db.Users.FindAsync(currentUserId);
            if (updateUsername == null)
            {
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            }

            try
            {
                updateUsername.Username = newUsername;
                Logger.DEBUG.Log($"Username updated to {updateUsername.Username}");
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new
                    { Error = $"Failed to update username for user with id {currentUserId}. {e.Message}" });
            }

            return Ok(new { Message = "Successfully updated Username" });
        }

        [Authorize(Policy = "AccessToken")]
        [HttpPut("updatemail")]
        public async Task<IActionResult> UpdateUsermail(string newMail)
        {
            var currentUserId = UserInformation.GetCurrentUserId(User);
            if (currentUserId == -1) return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            var changeMail = await db.Users.FindAsync(currentUserId);
            if (changeMail == null)
            {
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            }

            try
            {
                changeMail.Email = newMail;
                Logger.DEBUG.Log($"Email updated to {changeMail.Email}");
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new
                    { Error = $"Failed to update email for user with id {currentUserId}. {e.Message}" });
            }

            return Ok(new { Message = "Successfully updated Email" });
        }

        [Authorize(Policy = "AccessToken")]
        [HttpPut("updatepassword")]
        public async Task<IActionResult> UpdateUserPassword(string newPassword)
        {
            var currentUserId = UserInformation.GetCurrentUserId(User);
            if (currentUserId == -1) return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            var changePassword = await db.Users.FindAsync(currentUserId);
            if (changePassword == null)
                return NotFound(new { UserNotFound = $"User with id {currentUserId} not found." });
            try
            {
                byte[] compareSalt = Convert.FromBase64String(changePassword.Salt);
                if (_passwordService.CompareHashPassword(_passwordService.HashPassword(newPassword, compareSalt),
                        changePassword.Password))
                {
                    Logger.INFO.Log("New Password is same as old one");
                    return BadRequest(new { Error = "New Password is same as old one" });
                }

                byte[] newSalt = _passwordService.createSalt();
                string saltBase64String = Convert.ToBase64String(newSalt);
                string newHashedPassword = _passwordService.HashPassword(newPassword, newSalt);
                changePassword.Password = newHashedPassword;
                changePassword.Salt = saltBase64String;
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new
                    { Error = $"Failed to update password for user with id {currentUserId}. {e.Message}" });
            }

            return Ok(new { Message = "Successfully updated User Password" });
        }
    }
}