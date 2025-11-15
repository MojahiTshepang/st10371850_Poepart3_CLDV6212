using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SleazyRetailers.Models
{
    public class Upload
    {
        [Required(ErrorMessage = "Please select a file")]
        [Display(Name = "Proof of Payment File")]
        [NotMapped] // ADD THIS LINE
        public IFormFile FileToUpload { get; set; }

        [Display(Name = "Related Order ID")]
        public string RelatedOrderId { get; set; }

        [Display(Name = "Customer Name (Optional)")]
        public string CustomerName { get; set; }
    }
}