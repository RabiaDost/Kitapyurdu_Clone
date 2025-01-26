namespace Kitapyurdu_Clone.Pages.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string ImagePath { get; set; }
        public int Quantity { get; set; }
        public string Type { get; set; }
    }
}
