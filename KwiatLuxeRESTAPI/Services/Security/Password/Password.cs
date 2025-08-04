using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace KwiatLuxeRESTAPI.Services.Security.Password
{
    public class Password : IPasswordHasher
    {
        public string HashPassword(string password, byte[] salt)
        {
            string hashedpassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: SetApiOptions.SetIterationCount,
                numBytesRequested: 256 / 8));
            return hashedpassword;
        }

        public byte[] createSalt()
        {
            byte[] test = RandomNumberGenerator.GetBytes(SetApiOptions.SetSaltBitSize / 8);
            return test;
        }

        public bool CompareHashPassword(string enteredPassword, string userPassword)
        {
            if (userPassword.Length != enteredPassword.Length) return false;
            bool success = false;
            for (int i = 0; i < enteredPassword.Length; i++)
            {
                if (userPassword[i] == enteredPassword[i])
                {
                    success = true;
                }
            }
            return success;
        }
    }
}