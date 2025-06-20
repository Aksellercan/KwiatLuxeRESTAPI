namespace KwiatLuxeRESTAPI.Services.Security
{
    public interface IPasswordHasher
    {
        string HashPassword(string password, byte[] salt, int iterationCount);
        byte[] createSalt(int bits);
    }
}
