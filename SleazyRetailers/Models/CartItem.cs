using System.ComponentModel.DataAnnotations;

namespace SleazyRetailers.Models
{
    public class CartItem
    {
        public string ProductId { get; set; }

        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        public double Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        [Display(Name = "Total")]
        public decimal Total => (decimal)(Price * Quantity);
    }
}