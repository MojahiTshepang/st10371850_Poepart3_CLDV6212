using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SleazyRetailers.Models
{
    public class Contract
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Contract name is required")]
        [Display(Name = "Contract Name")]
        public string ContractName { get; set; }

        [Display(Name = "Contract Type")]
        public string ContractType { get; set; } = ContractTypes.General;

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "File Size")]
        public long? FileSize { get; set; }

        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Contract Value")]
        [Range(0, double.MaxValue, ErrorMessage = "Contract value must be positive")]
        public decimal? ContractValue { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Contract Party")]
        public string ContractParty { get; set; }

        [Required(ErrorMessage = "Contract file is required")]
        [Display(Name = "Contract File")]
        [NotMapped] // ADD THIS LINE - tells EF to ignore this property
        public IFormFile ContractFile { get; set; }
    }
}