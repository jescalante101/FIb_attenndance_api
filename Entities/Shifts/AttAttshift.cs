using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Shifts
{
    // AttAttshift Entity
    [Table("att_attshift")]
    public class AttAttshift
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("alias")]
        [StringLength(50)]
        [Required]
        public string Alias { get; set; } = string.Empty;

        [Column("cycle_unit")]
        [Required]
        public short CycleUnit { get; set; }

        [Column("shift_cycle")]
        [Required]
        public int ShiftCycle { get; set; }

        [Column("work_weekend")]
        [Required]
        public bool WorkWeekend { get; set; }

        [Column("weekend_type")]
        [Required]
        public short WeekendType { get; set; }

        [Column("work_day_off")]
        [Required]
        public bool WorkDayOff { get; set; }

        [Column("day_off_type")]
        [Required]
        public short DayOffType { get; set; }

        [Column("auto_shift")]
        [Required]
        public bool AutoShift { get; set; }

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
        public virtual ICollection<AttShiftdetail> AttShiftdetails { get; set; } = new List<AttShiftdetail>();
    }

}
