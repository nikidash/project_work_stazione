using Microsoft.EntityFrameworkCore;
using FerroviaApi.Models;

namespace FerroviaApi.Data
{
    public class FerroviaApiDbContext : DbContext
    {
        public FerroviaApiDbContext(DbContextOptions<FerroviaApiDbContext> options)
            : base(options) { }

        // CORREZIONE: Uso il nome completo per Semaphore
        public DbSet<FerroviaApi.Models.Semaphore> Semaphores { get; set; }

        // Altri DbSet
        public DbSet<Sector> Sectors { get; set; }
        public DbSet<Switch> Switches { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<TrainsLog> TrainsLogs { get; set; }
        public DbSet<Maintenance> Maintenance { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SWITCHES: Mappatura al nome SQL e configurazione FK multiple
            modelBuilder.Entity<Switch>().ToTable("switches");

            // Relazioni Switch -> Sector (OnDelete RESTRICT)
            modelBuilder.Entity<Switch>()
                .HasOne(s => s.InputSector)
                .WithMany(sec => sec.InputSwitches)
                .HasForeignKey(s => s.InputId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Switch>()
                .HasOne(s => s.StraightSector)
                .WithMany(sec => sec.StraightSwitches)
                .HasForeignKey(s => s.StraightId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Switch>()
                .HasOne(s => s.DeviationSector)
                .WithMany(sec => sec.DeviationSwitches)
                .HasForeignKey(s => s.DeviationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relazione Maintenance -> Sector (OnDelete SET NULL)
            modelBuilder.Entity<Maintenance>()
               .HasOne(m => m.MaintenanceSector)
               .WithMany(s => s.MaintenancePlatforms)
               .HasForeignKey(m => m.MaintenancePlatform)
               .OnDelete(DeleteBehavior.SetNull);

            // Settori: Vincolo UNIQUE su platform_number
            modelBuilder.Entity<Sector>()
                .HasIndex(s => s.PlatformNumber)
                .IsUnique();
        }
    }
}