namespace KwiatLuxeRESTAPI.DTOs
{
    public class ProductDTO
    {
        public string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public string? FileImageUrl { get; set; }
    }
}
