using System.ComponentModel.DataAnnotations;

namespace Dtos.OHLD
{
    public class ExternalOhldDto
    {
        [Required]
        public string HldCode { get; set; } = string.Empty;

        [Required]
        public string WndFrm { get; set; } = string.Empty;

        [Required]
        public string WndTo { get; set; } = string.Empty;

        [Required]
        public string IsCurYear { get; set; } = string.Empty;

        [Required]
        public string IgnrWnd { get; set; } = string.Empty;

        [Required]
        public string WeekNoRule { get; set; } = string.Empty;

        public List<ExternalHld1Dto> Hld1s { get; set; } = new List<ExternalHld1Dto>();
    }
}