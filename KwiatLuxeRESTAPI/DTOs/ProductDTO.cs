namespace KwiatLuxeRESTAPI.DTOs
{
    public class ProductDTO
    {
        public required string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public required decimal ProductPrice { get; set; }
        public IFormFile? FileImageUrl { get; set; }
    }
}
