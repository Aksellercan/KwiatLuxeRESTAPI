namespace KwiatLuxeRESTAPI.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public required int UserId { get; set; }
        public List<CartProduct> CartProducts { get; set; }
        public decimal TotalAmount { get; set; }
        public User User { get; set; }
    }
}
