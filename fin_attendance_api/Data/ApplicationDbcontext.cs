using Entities.Manager;
using Entities.ManualLog;
using Entities.Scire;
using Entities.Shifts;
using Microsoft.EntityFrameworkCore;

namespace FibAttendanceApi.Data
{
    public class ApplicationDbcontext:DbContext
    {
        public ApplicationDbcontext(DbContextOptions<ApplicationDbcontext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Add any additional model configurations here

            // Configure composite keys (Data Annotations handle most of this, but for clarity)
            modelBuilder.Entity<AppUserSite>()
                .HasKey(e => new { e.UserId, e.SiteId });

            modelBuilder.Entity<SiteAreaCostCenter>()
                .HasKey(e => new { e.SiteId, e.AreaId });

            modelBuilder.Entity<SiteCostCenter>()
                .HasKey(e => new { e.SiteId, e.CostCenterId });

            modelBuilder.Entity<AttTimeintervalBreakTime>()
                .HasIndex(e => new { e.TimeintervalId, e.BreaktimeId })
                .IsUnique();

            modelBuilder.Entity<AttBreaktime>()
                .HasIndex(e => e.Alias)
                .IsUnique();

            // Configure default values (handled by Data Annotations where possible)
            modelBuilder.Entity<AppUserSite>()
                .Property(e => e.Active)
                .HasDefaultValue("Y");

            modelBuilder.Entity<EmployeeScheduleException>()
                .Property(e => e.ExceptionType)
                .HasDefaultValue((byte)1);

            modelBuilder.Entity<EmployeeScheduleException>()
                .Property(e => e.IsActive)
                .HasDefaultValue((byte)1);

            modelBuilder.Entity<EmployeeScheduleException>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("getdate()");

            modelBuilder.Entity<EmployeeScheduleException>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("getdate()");

            modelBuilder.Entity<SiteAreaCostCenter>()
                .Property(e => e.Active)
                .HasDefaultValue("Y");

            modelBuilder.Entity<SiteCostCenter>()
                .Property(e => e.Active)
                .HasDefaultValue("Y");


            modelBuilder.Entity<RhArea>(entity =>
            {
                entity.ToTable("RH_Area", "SCIRERH_V4.dbo");
                entity.HasKey(e => e.AreaId);
                entity.Property(e => e.AreaId).HasColumnName("Area_Id");
                entity.Property(e => e.CompaniaId).HasColumnName("Compania_Id");
            });

            modelBuilder.Entity<Ccosto>(entity =>
            {
                entity.ToTable("Ccosto", "SCIRERH_V4.dbo");
                entity.HasKey(e => e.CcostoId);
                entity.Property(e => e.CcostoId).HasColumnName("ccosto_id");
                entity.Property(e => e.EstadoId).HasColumnName("Estado_id");
                entity.Property(e => e.CompaniaId).HasColumnName("Compania_Id");
            });

        }
        //Scire Tables
        public DbSet<RhArea> RhAreas { get; set; }
        public DbSet<Ccosto> Ccostos { get; set; }

        // DbSets
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppUserSite> AppUserSites { get; set; }
        public DbSet<AttAttshift> AttAttshifts { get; set; }
        public DbSet<AttBreaktime> AttBreaktimes { get; set; }
        public DbSet<AttManuallog> AttManuallogs { get; set; }
        public DbSet<AttShiftdetail> AttShiftdetails { get; set; }
        public DbSet<AttTimeinterval> AttTimeintervals { get; set; }
        public DbSet<AttTimeintervalBreakTime> AttTimeintervalBreakTimes { get; set; }
        public DbSet<EmployeeScheduleException> EmployeeScheduleExceptions { get; set; }
        public DbSet<SiteAreaCostCenter> SiteAreaCostCenters { get; set; }
        public DbSet<SiteCostCenter> SiteCostCenters { get; set; }

        public DbSet<EmployeeShiftAssignment> EmployeeScheduleAssignments { get; set; }

    }
}
