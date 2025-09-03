using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.OHLD
{

    [Table("OHLD")]
    public partial class Ohld
    {
        [Key]
        [StringLength(20)]
        public string HldCode { get; set; } = null!;

        [StringLength(1)]
        public string? WndFrm { get; set; }

        [StringLength(1)]
        public string? WndTo { get; set; }

        [Column("isCurYear")]
        [StringLength(1)]
        public string? IsCurYear { get; set; }

        [Column("ignrWnd")]
        [StringLength(1)]
        public string? IgnrWnd { get; set; }

        [StringLength(1)]
        public string? WeekNoRule { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Column("updated_by")]
        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        public virtual ICollection<Hld1> Hld1s { get; set; }
    }
}
