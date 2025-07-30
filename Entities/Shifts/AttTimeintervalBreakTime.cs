using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Shifts
{
    // AttTimeintervalBreakTime Entity
    [Table("att_timeinterval_break_time")]
    public class AttTimeintervalBreakTime
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("timeinterval_id")]
        [Required]
        public int TimeintervalId { get; set; }

        [Column("breaktime_id")]
        [Required]
        public int BreaktimeId { get; set; }

        // Navigation Properties
        [ForeignKey("TimeintervalId")]
        public virtual AttTimeinterval? Timeinterval { get; set; }

        [ForeignKey("BreaktimeId")]
        public virtual AttBreaktime? Breaktime { get; set; }
    }

}
