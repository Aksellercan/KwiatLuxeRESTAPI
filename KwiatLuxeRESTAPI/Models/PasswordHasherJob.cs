namespace KwiatLuxeRESTAPI.Models
{
    public class PasswordHasherJob
    {
        public required string Id { get; set; }
        public required string Input { get; set; }
        public string? Result { get; set; }
        public required PasswordHasherEnum.Status Status { get; set; }
    }
}
