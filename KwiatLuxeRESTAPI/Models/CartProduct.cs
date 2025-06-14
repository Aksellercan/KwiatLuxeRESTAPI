namespace KwiatLuxeRESTAPI.Models
{
    public class CartProduct
    {
        public int CartId { get; set; } //tied to userId in cart
        public Cart Cart { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }
}
