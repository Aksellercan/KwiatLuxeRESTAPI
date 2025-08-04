using KwiatLuxeRESTAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KwiatLuxeRESTAPI.Services.Security.Authorization
{
    public class JWTValidation(IConfiguration config)
    {
        public string GenerateAccessToken(User user, int expireDays)
        {
            var getKey = config["Jwt:Key"];
            if (getKey == null)
            {
                throw new Exception("JWT Key not set");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(getKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
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
            var getKey = config["Jwt:Key"];
            if (getKey == null)
            {
                throw new Exception("JWT Key not set");
            }
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(getKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var refreshToken = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
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
                var getKey = config["Jwt:Key"];
                if (getKey == null)
                {
                    throw new Exception("JWT Key not set");
                }
                TokenValidationParameters validationParameters = new()
                {
                    ValidateLifetime = true,
                    ValidAudience = config["Jwt:Audience"],
                    ValidIssuer = config["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(getKey))
                };
                ClaimsPrincipal claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, validationParameters, out SecurityToken validatedToken);
                return claimsPrincipal;
            }
            catch (Exception e)
            {
                throw new Exception($"Error validating. Error details: {e}");
            }
        }
    }
}
