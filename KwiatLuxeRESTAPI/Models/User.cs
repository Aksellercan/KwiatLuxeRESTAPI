namespace KwiatLuxeRESTAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Salt { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public List<Order>? Orders { get; set; }
        public Cart? Cart { get; set; }
        public Token? Token { get; set; }
    }
}