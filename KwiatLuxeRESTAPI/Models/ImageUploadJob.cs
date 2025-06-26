using KwiatLuxeRESTAPI.DTOs;

namespace KwiatLuxeRESTAPI.Models
{
    public class ImageUploadJob
    {
        public required string Id { get; set; }
        public int? updateProduct { get; set; }
        public ProductDTO? ProductDto { get; set; }
        public required IFormFile FileUpload { get; set; }
        public required BackgroundJobStatus Status { get; set; }
    }
}
