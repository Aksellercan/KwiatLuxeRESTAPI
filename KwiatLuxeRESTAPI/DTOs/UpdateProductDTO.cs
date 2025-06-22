namespace KwiatLuxeRESTAPI.DTOs
{
    public class UpdateProductDTO
    {
        public string? ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public decimal? ProductPrice { get; set; }
        public IFormFile? FileImageUrl { get; set; }
    }
}
