using KwiatLuxeRESTAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KwiatLuxeRESTAPI.Services.Security.Authorization
{
    public class JWTValidation
    {
        private readonly IConfiguration _config;

        public JWTValidation(IConfiguration config) 
        {
            _config = config;
        }

        public string GenerateAccessToken(User user, int expireDays)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
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

        public string GenerateRefreshToken(User user, int expireDays)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
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

        public ClaimsPrincipal ValidateToken(string jwtToken)
        {
            try
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
            catch (Exception e)
            {
                throw new Exception($"Error validating. Error details: {e}");
            }
        }
    }
}
