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
using System.Security.Claims;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;
        private readonly IConfiguration _config;
        private Password _passwordService = new();
        private bool USE_COOKIES = SetAPIOptions.USE_COOKIES;
        private UserInformation _userInformation = new();
        private readonly IMemoryCache _memoryCache;
        private JWTValidation _jwtValidation;

        public AuthController(KwiatLuxeDb db, IConfiguration config, IMemoryCache memoryCache)
        {
            _db = db;
            _config = config;
            _memoryCache = memoryCache;
            _jwtValidation = new(_config);
        }

        private void CookieOptions(string? text, bool removeCookie) 
        {
            if (removeCookie) 
            {
                Response.Cookies.Delete(SetAPIOptions.COOKIE_NAME, new CookieOptions
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
                    Logger.Log(Severity.ERROR, "Append Cookie Text Cannot be NULL");
                    return; 
                }
                Response.Cookies.Append(SetAPIOptions.COOKIE_NAME, text, new CookieOptions
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
            byte[] salt = _passwordService.createSalt(256);
            string saltBase64tring = Convert.ToBase64String(salt);

            var user = new User
            {
                Username = userRegister.Username,
                Password = userRegister.Password,
                Salt = saltBase64tring,
                Role =  SetAPIOptions.DEFAULT_ROLE,
                Email = userRegister.Email
            };
            user.Password = _passwordService.HashPassword(userRegister.Password, salt);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok( new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO userLogin)
        {
            // Get first matching user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userLogin.Username);
            if (user == null) return NotFound("login error: User not found");
            // Retrieve saved Salt for comparing hashes
            byte[] salt = Convert.FromBase64String(user.Salt);
            if (!_passwordService.CompareHashPassword(userLogin.Password, user.Password, salt)) return Unauthorized(new { UnAuthorized = "Wrong Login details"});

            var token = _jwtValidation.GenerateAccessToken(user, 1);
            if (USE_COOKIES)
            {
                CookieOptions(token, false);
                return Ok(new { loginSuccess = "Logged in Successfully" });
            }
            return Ok(new { AccessToken = token });
        }

        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshAccessToken([FromBody] TokenDTO tokenDTO)
        {
            if (tokenDTO.AccessToken == null && tokenDTO.RefreshToken == null) return BadRequest(new { error = "Token cannot be empty" });
            Claim? claimCurrentUserId = null;
            try
            {
                string? tokenType = null;
                if (String.IsNullOrWhiteSpace(tokenDTO.AccessToken) && !String.IsNullOrWhiteSpace(tokenDTO.RefreshToken))
                {
                    tokenType = tokenDTO.RefreshToken;
                }
                else if (String.IsNullOrWhiteSpace(tokenDTO.RefreshToken) && !String.IsNullOrWhiteSpace(tokenDTO.AccessToken))
                {
                    tokenType = tokenDTO.AccessToken;
                }
                if (tokenType == null) throw new Exception ("Token type could not be determined");
                claimCurrentUserId = _jwtValidation.ValidateToken(tokenType).FindFirst(ClaimTypes.NameIdentifier);
            } catch (Exception e) 
            {
                Logger.Log(Severity.ERROR, $"{e}");
                return BadRequest(new { error = $"{e.ToString()}"});
            }
            if (claimCurrentUserId == null) return Unauthorized(new { UnAuthorized = "No user ID claim found." });
            int parsedClaimId = _userInformation.GetCurrentUserId(claimCurrentUserId?.Value);
            var retrieveToken = await _db.Tokens.Where(t => t.UserId == parsedClaimId).FirstOrDefaultAsync();
            if (retrieveToken != null) {
                if (retrieveToken.RevokedAt == null)
                {
                    if (DateTime.UtcNow < retrieveToken.ExpiresAt) return Ok(new { refreshToken = retrieveToken.RefreshToken, expiresAt = retrieveToken.ExpiresAt });
                    _db.Tokens.Remove(retrieveToken);
                    await _db.SaveChangesAsync();
                    return BadRequest(new { error = "Refresh Token Expired" });
                }
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == parsedClaimId);
            if (user == null) return Unauthorized(new { UnAuthorized = "User not found." });
            var token = _jwtValidation.GenerateRefreshToken(user, 7); //7 day token
            if (USE_COOKIES)
            {
                CookieOptions(token, false);
            }
            if ((user != null) && (retrieveToken != null))
            {
                retrieveToken.RefreshToken = token;
                retrieveToken.ExpiresAt = DateTime.UtcNow.AddDays(7);
                retrieveToken.RevokedAt = null;
                retrieveToken.CreatedAt = DateTime.UtcNow;
                _db.Tokens.Update(retrieveToken);
                await _db.SaveChangesAsync();
            }
            else 
            {
                var tokenObj = new Token
                {
                    UserId = parsedClaimId,
                    RefreshToken = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _db.Tokens.Add(tokenObj);
                await _db.SaveChangesAsync();
            }
            return Ok(new { refreshToken = token, expiresAt = DateTime.UtcNow.AddDays(7) });
        }

        [HttpPost("exchangeToken")]
        [Authorize(Policy = "RefreshToken")]
        public async Task<IActionResult> ExchangeRefreshToken() 
        {
            int claimCurrentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (claimCurrentUserId == -1) return NotFound(new { UserNotFound = $"User with id {claimCurrentUserId} not found." });
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == claimCurrentUserId);
            if (user == null) return Unauthorized(new { UserNotFound = $"User with id {claimCurrentUserId} not found." });
            var token = _jwtValidation.GenerateAccessToken(user, 1); //return normal token 1 day
            if (USE_COOKIES)
            {
                CookieOptions(token, false);
                return Ok(new { tokenMessage = "Token Exchanged" });
            }
            return Ok(new { accessToken = token });
        }

        [Authorize(Policy = "AccessToken")]
        [HttpPost("logout")]
        public IActionResult ClearCookiesLogOut()
        {
            if (!USE_COOKIES) return BadRequest(new { Error = $"USE_COOKIES is set to {USE_COOKIES}" });
            CookieOptions(null, true);
            return Ok(new { Message = "Logged out and cleared Cookies" });
        }

        [HttpGet("CurrentUser")]
        [Authorize(Policy = "AccessToken")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var claimCurrentUsername = _userInformation.GetCurrentUsername(User.FindFirst(ClaimTypes.Name)?.Value);
            int claimCurrentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var claimCurrentUserEmail = _userInformation.GetCurrentMail(User.FindFirst(ClaimTypes.Email)?.Value);
            var claimCurrentUserRole = _userInformation.getCurrentUserRole(User.FindFirst(ClaimTypes.Role)?.Value);
            if (claimCurrentUserId == -1 || claimCurrentUsername == null || claimCurrentUserRole == null || claimCurrentUserEmail == null)
            {
                return Unauthorized(new { UnAuthorized = "Unauthenticated or user not found" });
            }
            UserDTO? userCache;
            if (!_memoryCache.TryGetValue(claimCurrentUserId, out userCache))
            {
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
                    Id=user.Id,
                    Username=user.Username,
                    Email=user.Email,
                    Role=user.Role
                };
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                _memoryCache.Set(claimCurrentUserId, userCache, cacheEntryOptions);
            }
            return Ok(new { userInformation = userCache });
        }
        [HttpGet("isadmin")]
        [Authorize(Roles = "Admin", Policy = "AccessToken")]
        public IActionResult IsAdmin() 
        {
            var adminClaim = _userInformation.IsAdmin(User.FindFirst(ClaimTypes.Role)?.Value);
            if (adminClaim) 
            {
                return Ok(new { isAdmin = "Admin role verified"});
            }
            return Unauthorized(new { UnAuthorized = $"Admin Role can not be verified."});
        }
    }
}