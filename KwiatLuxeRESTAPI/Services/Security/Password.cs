using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using KwiatLuxeRESTAPI.Services.Logger;

namespace KwiatLuxeRESTAPI.Services.Security
{
    public class Password : IPasswordHasher
    {
        public string HashPassword(string password, byte[] salt, int iterationCount) 
        {
            string hashedpassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterationCount,
                numBytesRequested: 256 / 8));
            return hashedpassword;
        }

        public byte[] createSalt(int bits) 
        {
            byte[] test = RandomNumberGenerator.GetBytes(bits / 8);
            return test;
        }
    }
}