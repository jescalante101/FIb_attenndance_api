using System.ComponentModel.DataAnnotations;

namespace Dtos.OHLD
{
    public class ExternalHld1Dto
    {
        [Required]
        public string HldCode { get; set; } = string.Empty;

        [Required]
        public DateTime StrDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string Rmrks { get; set; } = string.Empty;
    }
}