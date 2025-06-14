namespace KwiatLuxeRESTAPI.DTOs
{
    public class OrderDTO
    {
        public List<OrderProductDTO> OrderProduct { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
