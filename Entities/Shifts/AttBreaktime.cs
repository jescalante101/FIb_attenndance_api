using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Shifts
{
    // AttBreaktime Entity
    [Table("att_breaktime")]
    public class AttBreaktime
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("alias")]
        [StringLength(50)]
        [Required]
        public string Alias { get; set; } = string.Empty;

        [Column("period_start")]
        [Required]
        public DateTime PeriodStart { get; set; }

        [Column("duration")]
        [Required]
        public int Duration { get; set; }

        [Column("func_key")]
        [Required]
        public short FuncKey { get; set; }

        [Column("available_interval_type")]
        [Required]
        public short AvailableIntervalType { get; set; }

        [Column("available_interval")]
        [Required]
        public int AvailableInterval { get; set; }

        [Column("multiple_punch")]
        [Required]
        public short MultiplePunch { get; set; }

        [Column("calc_type")]
        [Required]
        public short CalcType { get; set; }

        [Column("minimum_duration")]
        public int? MinimumDuration { get; set; }

        [Column("early_in")]
        [Required]
        public short EarlyIn { get; set; }

        [Column("end_margin")]
        [Required]
        public int EndMargin { get; set; }

        [Column("late_in")]
        [Required]
        public short LateIn { get; set; }

        [Column("min_early_in")]
        [Required]
        public int MinEarlyIn { get; set; }

        [Column("min_late_in")]
        [Required]
        public int MinLateIn { get; set; }

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
        public virtual ICollection<AttTimeintervalBreakTime> AttTimeintervalBreakTimes { get; set; } = new List<AttTimeintervalBreakTime>();
    }
}
