using KwiatLuxeRESTAPI.DTOs;
using KwiatLuxeRESTAPI.Models;
using KwiatLuxeRESTAPI.Services.Data;
using KwiatLuxeRESTAPI.Services.Logger;
using KwiatLuxeRESTAPI.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KwiatLuxeRESTAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly KwiatLuxeDb _db;
        private readonly IConfiguration _config;
        private Password _passwordService = new();
        private bool USE_COOKIES = false;
        private UserInformation _userInformation = new();
        private int iterationCount = 100000;
        private readonly IMemoryCache _memoryCache;

        public AuthController(KwiatLuxeDb db, IConfiguration config, IMemoryCache memoryCache)
        {
            _db = db;
            _config = config;
            _memoryCache = memoryCache;
        }

        private void CookieOptions(string? text, bool removeCookie) 
        {
            if (removeCookie) 
            {
                Response.Cookies.Delete("Identity", new CookieOptions
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
                Response.Cookies.Append("Identity", text, new CookieOptions
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
                Role =  "Customer",
                Email = userRegister.Email
            };
            user.Password = _passwordService.HashPassword(userRegister.Password, salt, iterationCount);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok( new { Message = "User registered successfully" });
        }

        private byte[] GetUserSaltDB(User user)
        {
            return Convert.FromBase64String(user.Salt);
        }

        private string GenerateJwtToken(User user, int expireDays, bool Generate_Refresh_Token)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            if (Generate_Refresh_Token) 
            {
                var refreshToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: new List<Claim>
                {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("Purpose", "RefreshToken")
                },
                expires: DateTime.UtcNow.AddDays(expireDays),
                signingCredentials: credentials);
                return new JwtSecurityTokenHandler().WriteToken(refreshToken);
            }
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("Purpose", "AccessToken")
                },
                expires: DateTime.UtcNow.AddDays(expireDays),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO userLogin)
        {
            // Get first matching user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == userLogin.Username);
            if (user == null) return NotFound("login error: User not found");
            // Retrieve saved Salt for comparing hashes
            byte[] salt = GetUserSaltDB(user);
            if (!CompareHashPassword(userLogin.Password, user.Password, salt)) return Unauthorized(new { UnAuthorized = "Wrong Login details"});

            var token = GenerateJwtToken(user, 1, false);
            if (USE_COOKIES)
            {
                CookieOptions(token, false);
                return Ok(new { loginSuccess = "Logged in Successfully" });
            }
            return Ok(new { AccessToken = token });
        }

        private bool CompareHashPassword(string enteredPassword, string userPassword, byte[] salt)
        {
            if (string.Equals(userPassword, _passwordService.HashPassword(enteredPassword, salt, iterationCount)))
            {
                return true;
            }
            return false;
        }

        private ClaimsPrincipal ValidateToken(string jwtToken) 
        {
            SecurityToken validatedToken;
            TokenValidationParameters validationParameters = new TokenValidationParameters();
            validationParameters.ValidateLifetime = true;
            validationParameters.ValidAudience = _config["Jwt:Audience"];
            validationParameters.ValidIssuer = _config["Jwt:Issuer"];
            validationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            ClaimsPrincipal claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, validationParameters, out validatedToken);
            return claimsPrincipal;
        }

        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshAccessToken([FromBody] TokenDTO tokenDTO)
        {
            Claim claimCurrentUserId = null;
            if (tokenDTO.AccessToken == null)
            {
                claimCurrentUserId = ValidateToken(tokenDTO.RefreshToken).FindFirst(ClaimTypes.NameIdentifier);
            }
            else if (tokenDTO.RefreshToken == null) 
            {
                claimCurrentUserId = ValidateToken(tokenDTO.AccessToken).FindFirst(ClaimTypes.NameIdentifier);
            }
            if (claimCurrentUserId == null) return Unauthorized(new { UnAuthorized = "No user ID claim found." });
            int parsedClaimId = int.Parse(claimCurrentUserId?.Value);
            var retrieveToken = await _db.Tokens.Where(t => t.UserId == parsedClaimId).FirstOrDefaultAsync();
            if (retrieveToken != null && retrieveToken.RevokedAt == null)
            {
                if (DateTime.UtcNow < retrieveToken.ExpiresAt) return Ok(new { refreshToken = retrieveToken.RefreshToken, expiresAt = retrieveToken.ExpiresAt });
                return BadRequest(new { error = "Refresh Token Expired"});
            }
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == parsedClaimId);
            if (user == null) return Unauthorized(new { UnAuthorized = "User not found." });
            var token = GenerateJwtToken(user, 7, true); //7 day token
            if (USE_COOKIES)
            {
                CookieOptions(token, false);
            }
            if ((user != null) && (retrieveToken != null))
            {
                retrieveToken.RefreshToken = token;
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
            return Ok(new { refreshToken = token });
        }

        [HttpPost("exchangeToken")]
        [Authorize(Policy = "RefreshToken")]
        public async Task<IActionResult> ExchangeRefreshToken() 
        {
            int claimCurrentUserId = _userInformation.GetCurrentUserId(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (claimCurrentUserId == -1) return NotFound(new { UserNotFound = $"User with id {claimCurrentUserId} not found." });
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == claimCurrentUserId);
            if (user == null) return Unauthorized(new { UserNotFound = $"User with id {claimCurrentUserId} not found." });
            var token = GenerateJwtToken(user, 1, false); //return normal token 1 day
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
            if (!USE_COOKIES) return BadRequest(new { Error = $"USE_COOKIES is {USE_COOKIES}" });
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
            UserDTO userCache;
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
        [Authorize (Roles="Admin", Policy="AccessToken")]
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