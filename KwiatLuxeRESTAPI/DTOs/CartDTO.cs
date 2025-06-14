namespace KwiatLuxeRESTAPI.DTOs
{
    public class CartDTO
    {
            public List<OrderProductDTO> CartProduct { get; set; }
            public decimal TotalAmount { get; set; }
    }
}
