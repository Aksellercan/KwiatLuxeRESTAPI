using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace KwiatLuxeRESTAPI.Services.Security.Password
{
    public class Password : IPasswordHasher
    {
        public string HashPassword(string password, byte[] salt) 
        {
            string hashedpassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: SetAPIOptions.SET_ITERATION_COUNT,
                numBytesRequested: 256 / 8));
            return hashedpassword;
        }

        public byte[] createSalt() 
        {
            byte[] test = RandomNumberGenerator.GetBytes(SetAPIOptions.SET_SALT_BIT_SIZE / 8);
            return test;
        }

        public bool CompareHashPassword(string enteredPassword, string userPassword) 
        {
            if (enteredPassword == null) return false;
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