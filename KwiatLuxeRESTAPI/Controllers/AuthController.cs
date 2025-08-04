using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.Logger;
using KwiatLuxeRESTAPI.Services.Security.Authorization;
using KwiatLuxeRESTAPI.Services.Security.Password;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;
        private readonly Password _passwordService;
        private readonly bool _useCookies = SetApiOptions.UseCookies;
        private readonly IMemoryCache _memoryCache;
        private readonly JWTValidation _jwtValidation;
        private readonly Channel<UserRegisterJob> _registerChannel;
        private readonly ConcurrentDictionary<string, BackgroundJobStatus> _registerStatus;

        public AuthController(KwiatLuxeDb db, IConfiguration config, IMemoryCache memoryCache, Password passwordService,
            Channel<UserRegisterJob> registerChannel, ConcurrentDictionary<string, BackgroundJobStatus> registerStatus)
        {
            _registerChannel = registerChannel;
            _registerStatus = registerStatus;
            _db = db;
            _memoryCache = memoryCache;
            _jwtValidation = new JWTValidation(config);
            _passwordService = passwordService;
        }

        private void CookieOptions(string? text, bool removeCookie)
        {
            if (removeCookie)
            {
                Response.Cookies.Delete(SetApiOptions.CookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(-1)
                });
            }
            else
            {
                if (text == null)
                {
                    Logger.ERROR.Log("Append Cookie Text Cannot be NULL");
                    return;
                }

                Response.Cookies.Append(SetApiOptions.CookieName, text, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Secure = false,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(1)
                });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO userRegister)
        {
            var usernameCheck = await _db.Users.Where(u => u.Username == userRegister.Username)
                .Select(u => new { u.Username }).FirstOrDefaultAsync();
            if (usernameCheck != null)
                return BadRequest(new { UserExists = $"User with username {usernameCheck.Username} already exists" });
            var processId = Guid.NewGuid().ToString();
            var userRegisterJob = new UserRegisterJob
            {
                Id = processId,
                UserRegisterDto = userRegister,
                Status = BackgroundJobStatus.Queued
            };
            await _registerChannel.Writer.WriteAsync(userRegisterJob);
            _registerStatus[processId] = BackgroundJobStatus.Queued;
            var request = HttpContext.Request;
            return Created($"{request.Scheme}://{request.Host}/Auth/registerQueue/{processId}",
                new
                {
                    processId,
                    processStatus = BackgroundJobStatus.Queued,
                    queueHelper = "added to queue"
                });
        }

        [HttpGet("registerQueue/{processid}")]
        public IActionResult GetRegisterStatus([FromRoute] string processid)
        {
            if (!_registerStatus.ContainsKey(processid))
            {
                return BadRequest(new { QueueError = $"Job Id {processid} does not exist." });
            }

            return Ok(new { JobId = processid, Status = _registerStatus[processid].ToString() });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO userLogin)
        {
            // Get first matching user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userLogin.Username);
            if (user == null) return NotFound(new { UserNotFound = "User not found" });
            // Retrieve saved Salt for comparing hashes
            byte[] salt = Convert.FromBase64String(user.Salt);
            if (!_passwordService.CompareHashPassword(_passwordService.HashPassword(userLogin.Password, salt),
                    user.Password))
            {
                return Unauthorized(new { UnAuthorized = "Wrong Login details" });
            }

            var token = _jwtValidation.GenerateAccessToken(user, 1);
            if (!_useCookies) return Ok(new { AccessToken = token, ExpiresAt = DateTime.UtcNow.AddDays(1) });
            CookieOptions(token, false);
            return Ok(new { loginSuccess = "Logged in Successfully" });
        }

        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshAccessToken([FromBody] TokenDTO tokenDto)
        {
            if (tokenDto.AccessToken == null && tokenDto.RefreshToken == null)
                return BadRequest(new { error = "Token cannot be empty" });
            int? parsedClaimId;
            try
            {
                string? tokenType = null;
                if (string.IsNullOrWhiteSpace(tokenDto.AccessToken) &&
                    !string.IsNullOrWhiteSpace(tokenDto.RefreshToken))
                {
                    tokenType = tokenDto.RefreshToken;
                }
                else if (string.IsNullOrWhiteSpace(tokenDto.RefreshToken) &&
                         !string.IsNullOrWhiteSpace(tokenDto.AccessToken))
                {
                    tokenType = tokenDto.AccessToken;
                }

                if (tokenType == null) throw new Exception("Token type could not be determined");
                parsedClaimId = UserInformation.GetCurrentUserId(_jwtValidation.ValidateToken(tokenType));
                if (parsedClaimId == null) return Unauthorized(new { UnAuthorized = "No claim id found." });
            }
            catch (Exception e)
            {
                Logger.ERROR.Log($"{e}");
                return BadRequest(new { error = $"{e}" });
            }

            var retrieveToken = await _db.Tokens.Where(t => t.UserId == parsedClaimId).FirstOrDefaultAsync();
            if (retrieveToken is { RevokedAt: null })
            {
                if (DateTime.UtcNow < retrieveToken.ExpiresAt)
                    return Ok(
                        new { refreshToken = retrieveToken.RefreshToken, expiresAt = retrieveToken.ExpiresAt });
                _db.Tokens.Remove(retrieveToken);
                await _db.SaveChangesAsync();
                return BadRequest(new { error = "Refresh Token Expired" });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == parsedClaimId);
            if (user == null) return Unauthorized(new { UnAuthorized = "User not found." });
            var token = _jwtValidation.GenerateRefreshToken(user, 7); //7 day token
            if (_useCookies)
            {
                CookieOptions(token, false);
            }

            if (retrieveToken != null)
            {
                retrieveToken.RefreshToken = token;
                retrieveToken.ExpiresAt = DateTime.UtcNow.AddDays(7);
                retrieveToken.RevokedAt = null;
                retrieveToken.CreatedAt = DateTime.UtcNow;
                _db.Tokens.Update(retrieveToken);
            }
            else
            {
                var tokenObj = new Token
                {
                    UserId = user.Id,
                    RefreshToken = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _db.Tokens.Add(tokenObj);
            }

            await _db.SaveChangesAsync();
            return Ok(new { refreshToken = token, expiresAt = DateTime.UtcNow.AddDays(7) });
        }

        [HttpPost("exchangeToken")]
        [Authorize(Policy = "RefreshToken")]
        public async Task<IActionResult> ExchangeRefreshToken()
        {
            int? claimCurrentUserId = UserInformation.GetCurrentUserId(User);
            if (claimCurrentUserId == null)
                return NotFound(new { UserNotFound = $"User with id {claimCurrentUserId} not found." });
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == claimCurrentUserId);
            if (user == null)
                return Unauthorized(new { UserNotFound = $"User with id {claimCurrentUserId} not found." });
            var token = _jwtValidation.GenerateAccessToken(user, 1); //return normal token 1 day
            if (!_useCookies) return Ok(new { accessToken = token });
            CookieOptions(token, false);
            return Ok(new { tokenMessage = "Token Exchanged" });
        }

        [Authorize(Policy = "AccessToken")]
        [HttpPost("logout")]
        public IActionResult ClearCookiesLogOut()
        {
            if (!_useCookies) return BadRequest(new { Error = $"USE_COOKIES is set to {_useCookies}" });
            CookieOptions(null, true);
            return Ok(new { Message = "Logged out and cleared Cookies" });
        }

        [HttpGet("CurrentUser")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> GetCurrentUser()
        {
            int? claimCurrentUserId = UserInformation.GetCurrentUserId(User);
            string? claimCurrentUsername = UserInformation.GetCurrentUsername(User);
            string? claimCurrentUserEmail = UserInformation.GetCurrentMail(User);
            string? claimCurrentUserRole = UserInformation.GetCurrentUserRole(User);
            if (claimCurrentUserId == null || claimCurrentUsername == null || claimCurrentUserRole == null ||
                claimCurrentUserEmail == null)
            {
                return Unauthorized(new { UnAuthorized = "Unauthenticated or user not found" });
            }

            if (_memoryCache.TryGetValue(claimCurrentUserId, out UserDTO? userCache))
                return Ok(new
                    { userInformation = userCache, detailsCached = true, cacheExpiresIn = DateTime.Now.AddMinutes(5) });
            var user = await _db.Users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role
            }).FirstOrDefaultAsync(u => u.Id == claimCurrentUserId);
            if (user == null) return Unauthorized(new { UnAuthorized = "Unauthenticated or user not found" });
            userCache = new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            };
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            _memoryCache.Set(claimCurrentUserId, userCache, cacheEntryOptions);

            return Ok(new { userInformation = userCache, detailsCached = false });
        }

        [HttpGet("isadmin")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public IActionResult IsAdmin()
        {
            bool adminClaim = UserInformation.IsAdmin(User);
            if (adminClaim)
            {
                return Ok(new { isAdmin = "Admin role verified" });
            }

            return Unauthorized(new { UnAuthorized = $"Admin Role can not be verified." });
        }
    }
}