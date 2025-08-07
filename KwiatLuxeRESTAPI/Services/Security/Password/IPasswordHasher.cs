namespace KwiatLuxeRESTAPI.Services.Security.Password
{
    public interface IPasswordHasher
    {
        string HashPassword(string password, byte[] salt);
        byte[] CreateSalt();
        bool CompareHashPassword(string enteredPassword, string userPassword);
    }
}
