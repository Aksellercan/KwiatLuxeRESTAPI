namespace KwiatLuxeRESTAPI.Models
{
    public class Cart
    {
        public int UserId { get; set; }
        public List<CartProduct> CartProducts { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
