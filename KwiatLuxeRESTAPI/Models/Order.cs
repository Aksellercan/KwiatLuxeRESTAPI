namespace KwiatLuxeRESTAPI.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public List<OrderProduct>? OrderProducts { get; set; }
        public User? User { get; set; }
    }
}
