using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Shifts
{
    // AttShiftdetail Entity
    [Table("att_shiftdetail")]
    public class AttShiftdetail
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("in_time")]
        [Required]
        public DateTime InTime { get; set; }

        [Column("out_time")]
        [Required]
        public DateTime OutTime { get; set; }

        [Column("day_index")]
        [Required]
        public int DayIndex { get; set; }

        [Column("shift_id")]
        [Required]
        public int ShiftId { get; set; }

        [Column("time_interval_id")]
        [Required]
        public int TimeIntervalId { get; set; }

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

        // Navigation Properties
        [ForeignKey("ShiftId")]
        public virtual AttAttshift? Shift { get; set; }

        [ForeignKey("TimeIntervalId")]
        public virtual AttTimeinterval? TimeInterval { get; set; }
    }
}
