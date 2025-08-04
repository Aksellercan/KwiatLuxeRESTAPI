namespace KwiatLuxeRESTAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public required decimal ProductPrice { get; set; }
        public string? FileImageUrl { get; set; }
        public List<OrderProduct>? OrderProducts { get; set; }
        public List<CartProduct>? CartProducts { get; set; }
    }
}
