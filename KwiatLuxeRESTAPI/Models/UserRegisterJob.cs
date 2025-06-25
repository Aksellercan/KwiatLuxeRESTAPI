using KwiatLuxeRESTAPI.DTOs;

namespace KwiatLuxeRESTAPI.Models
{
    public class UserRegisterJob
    {
        public required string Id { get; set; }
        public required UserRegisterDTO UserRegisterDto { get; set; }
        public required BackgroundJobStatus Status { get; set; }
    }
}
