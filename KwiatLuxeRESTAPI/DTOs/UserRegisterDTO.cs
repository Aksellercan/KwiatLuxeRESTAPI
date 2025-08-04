namespace KwiatLuxeRESTAPI.DTOs
{
    public class UserRegisterDTO
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
    }
}
