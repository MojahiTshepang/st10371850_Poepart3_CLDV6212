using System.ComponentModel.DataAnnotations;

namespace SleazyRetailers.Models
{
    public class Product
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Stock Available is required")]
        [Display(Name = "Stock Available")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }
    }
}