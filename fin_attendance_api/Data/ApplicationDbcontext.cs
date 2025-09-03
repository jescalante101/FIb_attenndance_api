using DocumentFormat.OpenXml.Wordprocessing;
using Entities.Compensatory;
using Entities.Manager;
using Entities.ManualLog;
using Entities.OHLD;
using Entities.Personal;
using Entities.Scire;
using Entities.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FibAttendanceApi.Data
{
    public class ApplicationDbcontext : DbContext
    {
        public ApplicationDbcontext(DbContextOptions<ApplicationDbcontext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== CONFIGURACIÓN DE TRIGGERS ==========
            ConfigureTriggeredTables(modelBuilder);

            // ========== TUS CONFIGURACIONES ESPECÍFICAS ==========

            // Configure composite keys
            modelBuilder.Entity<AppUserSite>()
                .HasKey(e => new { e.UserId, e.SiteId });

            modelBuilder.Entity<SiteAreaCostCenter>()
                .HasKey(e => new { e.SiteId, e.AreaId });

            modelBuilder.Entity<SiteCostCenter>()
                .HasKey(e => new { e.SiteId, e.CostCenterId });

            modelBuilder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            // Configure indexes
            modelBuilder.Entity<AttTimeintervalBreakTime>()
                .HasIndex(e => new { e.TimeintervalId, e.BreaktimeId })
                .IsUnique();

            modelBuilder.Entity<AttBreaktime>()
                .HasIndex(e => e.Alias)
                .IsUnique();

            // Configure default values
            modelBuilder.Entity<AppUserSite>()
                .Property(e => e.Active)
                .HasDefaultValue("Y");

            modelBuilder.Entity<EmployeeScheduleException>(entity =>
            {
                entity.Property(e => e.ExceptionType).HasDefaultValue((byte)1);
                entity.Property(e => e.IsActive).HasDefaultValue((byte)1);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("getdate()");
            });

            modelBuilder.Entity<SiteAreaCostCenter>()
                .Property(e => e.Active)
                .HasDefaultValue("Y");

            modelBuilder.Entity<SiteCostCenter>()
                .Property(e => e.Active)
                .HasDefaultValue("Y");

            // Table mappings with specific schemas
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

            // Relationships
            modelBuilder.Entity<Hld1>(entity =>
            {
                entity.HasKey(e => new { e.HldCode, e.StrDate, e.EndDate }).HasName("HLD1_PRIMARY");
                entity.HasOne(d => d.Ohld)
                      .WithMany(p => p.Hld1s)
                      .HasForeignKey(d => d.HldCode)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_HLD1_OHLD");
            });

            modelBuilder.Entity<Ohld>(entity =>
            {
                entity.HasKey(e => e.HldCode).HasName("OHLD_PRIMARY");
                entity.Property(e => e.IgnrWnd).IsFixedLength();
                entity.Property(e => e.IsCurYear).IsFixedLength();
                entity.Property(e => e.WeekNoRule).IsFixedLength();
                entity.Property(e => e.WndFrm).IsFixedLength();
                entity.Property(e => e.WndTo).IsFixedLength();
            });

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId);


            modelBuilder.Entity<PersonalEntity>(entity =>
            {

                entity.ToTable("Personal", table =>
                {
                    table.HasTrigger("trg_Personal_UpdatedAt");
                    table.HasTrigger("trg_Personal_Audit");
                });
                // CLAVE: Configurar la estrategia de generación de ID
                entity.Property(e => e.Id)
                    .UseIdentityColumn()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy",
                        SqlServerValueGenerationStrategy.IdentityColumn);


                // Solo configuraciones que no están en Data Annotations
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETDATE()");

                // Indexes (opcional - para mejorar performance)
                entity.HasIndex(e => e.BranchId)
                    .HasDatabaseName("IX_Personal_BranchId");

                entity.HasIndex(e => e.AreaId)
                    .HasDatabaseName("IX_Personal_AreaId");

                entity.HasIndex(e => e.CostCenterId)
                    .HasDatabaseName("IX_Personal_CostCenterId");
            });

            modelBuilder.Entity<EmployeeShiftAssignment>(entity =>
            {
                // Asegúrate de que la clave primaria esté definida
                entity.HasKey(e => e.AssignmentId);

                // Y muy importante, indícale que es generada por la base de datos
                entity.Property(e => e.AssignmentId)
                      .ValueGeneratedOnAdd(); // Esto le dice a EF que SQL Server asignará el valor
            });

        }

        /// <summary>
        /// Configura todas las entidades que tienen triggers en la base de datos
        /// para evitar conflictos con las cláusulas OUTPUT de Entity Framework
        /// </summary>
        private void ConfigureTriggeredTables(ModelBuilder modelBuilder)
        {
            // AppUser - 5 triggers (3 UPDATE, 1 INSERT, 1 DELETE)
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("AppUser", tb =>
                {
                    tb.HasTrigger("tr_AppUser_DateAudit");
                    tb.HasTrigger("TR_AppUser_Delete");
                    tb.HasTrigger("TR_AppUser_Insert");
                    tb.HasTrigger("TR_AppUser_Update");
                });
            });

            // AppUserSite - 4 triggers
            modelBuilder.Entity<AppUserSite>(entity =>
            {
                entity.ToTable("AppUserSite", tb =>
                {
                    tb.HasTrigger("tr_AppUserSite_DateAudit");
                    tb.HasTrigger("TR_AppUserSite_Delete");
                    tb.HasTrigger("TR_AppUserSite_Insert");
                    tb.HasTrigger("TR_AppUserSite_Update");
                });
            });

            // AttAttshift - 4 triggers
            modelBuilder.Entity<AttAttshift>(entity =>
            {
                entity.ToTable("att_attshift", tb =>
                {
                    tb.HasTrigger("tr_att_attshift_DateAudit");
                    tb.HasTrigger("TR_att_attshift_Delete");
                    tb.HasTrigger("TR_att_attshift_Insert");
                    tb.HasTrigger("TR_att_attshift_Update");
                });
            });

            // AttBreaktime - 4 triggers
            modelBuilder.Entity<AttBreaktime>(entity =>
            {
                entity.ToTable("att_breaktime", tb =>
                {
                    tb.HasTrigger("tr_att_breaktime_DateAudit");
                    tb.HasTrigger("TR_att_breaktime_Delete");
                    tb.HasTrigger("TR_att_breaktime_Insert");
                    tb.HasTrigger("TR_att_breaktime_Update");
                });
            });

            // AttManuallog - 4 triggers
            modelBuilder.Entity<AttManuallog>(entity =>
            {
                entity.ToTable("att_manuallog", tb =>
                {
                    tb.HasTrigger("tr_att_manuallog_DateAudit");
                    tb.HasTrigger("TR_att_manuallog_Delete");
                    tb.HasTrigger("TR_att_manuallog_Insert");
                    tb.HasTrigger("TR_att_manuallog_Update");
                });
            });

            // AttShiftdetail - 1 trigger (solo UPDATE)
            modelBuilder.Entity<AttShiftdetail>(entity =>
            {
                entity.ToTable("att_shiftdetail", tb =>
                {
                    tb.HasTrigger("tr_att_shiftdetail_DateAudit");
                });
            });

            // AttTimeinterval - 1 trigger (solo UPDATE)
            modelBuilder.Entity<AttTimeinterval>(entity =>
            {
                entity.ToTable("att_timeinterval", tb =>
                {
                    // Corrected to point to the new, unified trigger
                    tb.HasTrigger("trg_AuditAndUpdate_att_timeinterval");
                });

                entity.HasMany(t => t.AttTimeintervalBreakTimes)
              .WithOne(b => b.Timeinterval)
              .HasForeignKey(b => b.TimeintervalId)
              .OnDelete(DeleteBehavior.Cascade);


            });

            // AttTimeintervalBreakTime - 1 trigger (solo UPDATE)
            modelBuilder.Entity<AttTimeintervalBreakTime>(entity =>
            {
                entity.ToTable("att_timeinterval_break_time", tb =>
                {
                    tb.HasTrigger("tr_att_timeinterval_break_time_DateAudit");
                });
            });

            // EmployeeShiftAssignment - 4 triggers
            modelBuilder.Entity<EmployeeShiftAssignment>(entity =>
            {
                entity.ToTable("EmployeeShiftAssignments", tb =>
                {
                    tb.HasTrigger("tr_EmployeeShiftAssignments_DateAudit");
                    tb.HasTrigger("TR_EmployeeShiftAssignments_Delete");
                    tb.HasTrigger("TR_EmployeeShiftAssignments_Insert");
                    tb.HasTrigger("TR_EmployeeShiftAssignments_Update");
                });
            });

            // Hld1 - 1 trigger (solo UPDATE)
            modelBuilder.Entity<Hld1>(entity =>
            {
                entity.ToTable("HLD1", tb =>
                {
                    tb.HasTrigger("tr_HLD1_DateAudit");
                });
            });

            // Ohld - 1 trigger (solo UPDATE)
            modelBuilder.Entity<Ohld>(entity =>
            {
                entity.ToTable("OHLD", tb =>
                {
                    tb.HasTrigger("tr_OHLD_DateAudit");
                });
            });

            // Permissions - 4 triggers
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions", tb =>
                {
                    tb.HasTrigger("tr_Permissions_DateAudit");
                    tb.HasTrigger("TR_Permissions_Delete");
                    tb.HasTrigger("TR_Permissions_Insert");
                    tb.HasTrigger("TR_Permissions_Update");
                });
            });

            // SiteAreaCostCenter - 1 trigger (solo UPDATE)
            modelBuilder.Entity<SiteAreaCostCenter>(entity =>
            {
                entity.ToTable("SiteAreaCostCenter", tb =>
                {
                    tb.HasTrigger("tr_SiteAreaCostCenter_DateAudit");
                });
            });

            // SiteCostCenter - 1 trigger (solo UPDATE)
            modelBuilder.Entity<SiteCostCenter>(entity =>
            {
                entity.ToTable("SiteCostCenter", tb =>
                {
                    tb.HasTrigger("tr_SiteCostCenter_DateAudit");
                });
            });

            // UserPermissions - 3 triggers (1 UPDATE, 1 INSERT, 1 DELETE)
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.ToTable("UserPermissions", tb =>
                {
                    tb.HasTrigger("tr_UserPermissions_DateAudit");
                    tb.HasTrigger("TR_UserPermissions_Delete");
                    tb.HasTrigger("TR_UserPermissions_Insert");
                });
            });

            // CompensatoryDays - 4 triggers
            modelBuilder.Entity<CompensatoryDay>(entity =>
            {
                entity.ToTable("CompensatoryDays", tb =>
                {
                    tb.HasTrigger("TR_CompensatoryDays_Audit");
                });
            });

            modelBuilder.Entity<PersonnelWhitelist>(entity =>
            {
                // Esto le dice a EF Core que la tabla tiene triggers.
                // EF Core usará una técnica diferente para guardar los cambios
                // que es compatible con los triggers, evitando así el error de la cláusula OUTPUT.
                entity.ToTable("personnel_whitelist", tb => {
                    tb.HasTrigger("trg_personnel_whitelist_update");
                    tb.HasTrigger("TR_personnel_whitelist_Audit");
                });
            });


        }

        //--- DbSets ---
        public DbSet<PersonnelWhitelist> PersonnelWhitelists { get; set; }

        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public virtual DbSet<Hld1> Hld1s { get; set; }
        public virtual DbSet<Ohld> Ohlds { get; set; }
        public DbSet<RhArea> RhAreas { get; set; }
        public DbSet<Ccosto> Ccostos { get; set; }
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

        public DbSet<PersonalEntity> Personals { get; set; }
        public DbSet<CompensatoryDay> CompensatoryDays { get; set; }
    }
}