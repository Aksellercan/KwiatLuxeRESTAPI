namespace KwiatLuxeRESTAPI.Models
{
    public class Token
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public User? User { get; set; }
    }
}
