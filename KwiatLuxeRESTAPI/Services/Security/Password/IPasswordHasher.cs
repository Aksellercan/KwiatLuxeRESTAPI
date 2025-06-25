namespace KwiatLuxeRESTAPI.Services.Security.Password
{
    public interface IPasswordHasher
    {
        string HashPassword(string password, byte[] salt);
        byte[] createSalt();
        Task<bool> CompareHashPassword(string enteredPassword, string userPassword, byte[] salt);
    }
}
