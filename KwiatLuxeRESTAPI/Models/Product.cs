namespace KwiatLuxeRESTAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public string? FileImageUrl { get; set; }
        public List<OrderProduct> OrderProducts { get; set; }
        public List<CartProduct> CartProducts { get; set; }
    }
}
