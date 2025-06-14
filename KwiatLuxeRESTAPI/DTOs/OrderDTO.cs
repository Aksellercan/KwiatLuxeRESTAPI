namespace KwiatLuxeRESTAPI.DTOs
{
    public class OrderDTO
    {
        //public int UserId { get; set; }
        public List<OrderProductDTO> OrderProduct { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
