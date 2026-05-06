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
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity?.GetTableName()?.ToLower());
            }

            modelBuilder.Entity<Sector>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Length).HasColumnName("length");
                entity.Property(e => e.IsAvailable).HasColumnName("is_available");
                entity.Property(e => e.IsStop).HasColumnName("is_stop");
                entity.Property(e => e.PlatformNumber).HasColumnName("platform_number");
            });

            modelBuilder.Entity<FerroviaApi.Models.Semaphore>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IsWorking).HasColumnName("is_working");
                entity.Property(e => e.IsGreen).HasColumnName("is_green");
                entity.Property(e => e.IsDeviating).HasColumnName("is_deviating");
            });

            modelBuilder.Entity<Switch>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.InputId).HasColumnName("input_id");
                entity.Property(e => e.StraightId).HasColumnName("straight_id");
                entity.Property(e => e.DeviationId).HasColumnName("deviation_id");
                entity.Property(e => e.SemaphoreId).HasColumnName("semaphore_id");
                entity.Property(e => e.IsWorking).HasColumnName("is_working");
                entity.Property(e => e.IsDeviating).HasColumnName("is_deviating");
            });

            modelBuilder.Entity<Train>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TrainNumber).HasColumnName("train_number");
                entity.Property(e => e.Length).HasColumnName("length");
                entity.Property(e => e.Origin).HasColumnName("origin");
                entity.Property(e => e.Destination).HasColumnName("destination");
                entity.Property(e => e.TrainType).HasColumnName("train_type");
                entity.Property(e => e.ScheduledArrivalTime).HasColumnName("scheduled_arrival_time");
                entity.Property(e => e.ScheduledDepartureTime).HasColumnName("scheduled_departure_time");
                entity.Property(e => e.IsTransit).HasColumnName("is_transit");
                entity.Property(e => e.AssignedPlatform).HasColumnName("assigned_platform");
            });

            modelBuilder.Entity<TrainsLog>(entity =>
            {
                entity.ToTable("trains_log"); 

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TrainNumber).HasColumnName("train_number");
                entity.Property(e => e.Length).HasColumnName("length");
                entity.Property(e => e.Origin).HasColumnName("origin");
                entity.Property(e => e.Destination).HasColumnName("destination");
                entity.Property(e => e.TrainType).HasColumnName("train_type");
                entity.Property(e => e.ScheduledArrivalTime).HasColumnName("scheduled_arrival_time");
                entity.Property(e => e.ScheduledDepartureTime).HasColumnName("scheduled_departure_time");
                entity.Property(e => e.IsTransit).HasColumnName("is_transit");
                entity.Property(e => e.ActualDepartureTime).HasColumnName("actual_departure_time");
                entity.Property(e => e.ActualArrivalTime).HasColumnName("actual_arrival_time");
                entity.Property(e => e.AssignedPlatform).HasColumnName("assigned_platform");
            });

            modelBuilder.Entity<Maintenance>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TrainId).HasColumnName("train_id");
                entity.Property(e => e.MaintenanceStart).HasColumnName("maintenance_start");
                entity.Property(e => e.MaintenanceEnd).HasColumnName("maintenance_end");
                entity.Property(e => e.MaintenancePlatform).HasColumnName("maintenance_platform");
            });

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