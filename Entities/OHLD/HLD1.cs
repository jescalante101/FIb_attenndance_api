using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;


namespace Entities.OHLD
{

    [PrimaryKey("HldCode", "StrDate", "EndDate")]
    [Table("HLD1")]
    public partial class Hld1
    {
        [Key]
        [StringLength(20)]
        public string HldCode { get; set; } = null!;

        [Key]
        [Column(TypeName = "datetime")]
        public DateTime StrDate { get; set; }

        [Key]
        [Column(TypeName = "datetime")]
        public DateTime EndDate { get; set; }

        [StringLength(50)]
        public string? Rmrks { get; set; }

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

        [ForeignKey("HldCode")]
        public virtual Ohld Ohld { get; set; } = null!;

    }
}
