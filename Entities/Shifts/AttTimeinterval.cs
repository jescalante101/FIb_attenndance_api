using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Entities.Shifts
{
    [Table("att_timeinterval")]
    public class AttTimeinterval
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("alias")]
        [StringLength(50)]
        [Required]
        public string Alias { get; set; } = string.Empty;

        [Column("use_mode")]
        [Required]
        public short UseMode { get; set; }

        [Column("in_time")]
        [Required]
        public DateTime InTime { get; set; }

        [Column("in_ahead_margin")]
        [Required]
        public int InAheadMargin { get; set; }

        [Column("in_above_margin")]
        [Required]
        public int InAboveMargin { get; set; }

        [Column("out_ahead_margin")]
        [Required]
        public int OutAheadMargin { get; set; }

        [Column("out_above_margin")]
        [Required]
        public int OutAboveMargin { get; set; }

        [Column("duration")]
        [Required]
        public int Duration { get; set; }

        [Column("in_required")]
        [Required]
        public short InRequired { get; set; }

        [Column("out_required")]
        [Required]
        public short OutRequired { get; set; }

        [Column("allow_late")]
        [Required]
        public int AllowLate { get; set; }

        [Column("allow_leave_early")]
        [Required]
        public int AllowLeaveEarly { get; set; }

        [Column("work_day")]
        [Required]
        public double WorkDay { get; set; }

        [Column("multiple_punch")]
        [Required]
        public short MultiplePunch { get; set; }

        [Column("available_interval_type")]
        [Required]
        public short AvailableIntervalType { get; set; }

        [Column("available_interval")]
        [Required]
        public int AvailableInterval { get; set; }

        [Column("work_time_duration")]
        [Required]
        public int WorkTimeDuration { get; set; }

        [Column("func_key")]
        [Required]
        public short FuncKey { get; set; }

        [Column("work_type")]
        [Required]
        public short WorkType { get; set; }

        [Column("day_change")]
        [Required]
        public DateTime DayChange { get; set; }

        [Column("early_in")]
        [Required]
        public short EarlyIn { get; set; }

        [Column("late_out")]
        [Required]
        public short LateOut { get; set; }

        [Column("min_early_in")]
        [Required]
        public int MinEarlyIn { get; set; }

        [Column("min_late_out")]
        [Required]
        public int MinLateOut { get; set; }

        [Column("overtime_lv")]
        [Required]
        public short OvertimeLv { get; set; }

        [Column("overtime_lv1")]
        [Required]
        public short OvertimeLv1 { get; set; }

        [Column("overtime_lv2")]
        [Required]
        public short OvertimeLv2 { get; set; }

        [Column("overtime_lv3")]
        [Required]
        public short OvertimeLv3 { get; set; }

        // --- NUEVAS PROPIEDADES AÑADIDAS ---

        [Column("overtime_lv1_percentage", TypeName = "decimal(5, 2)")]
        public decimal? OvertimeLv1Percentage { get; set; }

        [Column("overtime_lv2_percentage", TypeName = "decimal(5, 2)")]
        public decimal? OvertimeLv2Percentage { get; set; }

        [Column("overtime_lv3_percentage", TypeName = "decimal(5, 2)")]
        public decimal? OvertimeLv3Percentage { get; set; }

        [Column("compania_id")]
        [StringLength(15)]
        public string? CompaniaId { get; set; }

        // --- FIN DE NUEVAS PROPIEDADES ---

        [Column("total_markings")]
        public short? TotalMarkings { get; set; }

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

        [Column("rounding_threshold_minutes")]
        public int? RoundingThresholdMinutes { get; set; }


        // Navigation Properties
        public virtual ICollection<AttShiftdetail> AttShiftdetails { get; set; } = new List<AttShiftdetail>();
        public virtual ICollection<AttTimeintervalBreakTime> AttTimeintervalBreakTimes { get; set; } = new List<AttTimeintervalBreakTime>();
        public virtual ICollection<EmployeeScheduleException> EmployeeScheduleExceptions { get; set; } = new List<EmployeeScheduleException>();
    }
}