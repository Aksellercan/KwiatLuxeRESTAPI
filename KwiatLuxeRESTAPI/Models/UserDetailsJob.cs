using KwiatLuxeRESTAPI.DTOs;

namespace KwiatLuxeRESTAPI.Models
{
    public class UserDetailsJob
    {
        public required string Id { get; set; }
        public required int UserId { get; set; }
        public UserDTO UserResult { get; set; }
        public required BackgroundJobStatus Status { get; set; }
    }
}
