using Microsoft.EntityFrameworkCore;
using demoProject.Models;

namespace demoProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<TrackingUpdate> TrackingUpdates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasConversion<string>();
            });

            // Shipment configuration
            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TrackingNumber).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>();

                // Relationships
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentShipments)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AssignedDriver)
                    .WithMany(u => u.AssignedShipments)
                    .HasForeignKey(e => e.AssignedDriverId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // TrackingUpdate configuration
            modelBuilder.Entity<TrackingUpdate>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Shipment)
                    .WithMany(s => s.TrackingUpdates)
                    .HasForeignKey(e => e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany(u => u.TrackingUpdates)
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Notification configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.Shipment)
                    .WithMany(s => s.Notifications)
                    .HasForeignKey(e => e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Report configuration
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReportType).HasConversion<string>();

                entity.HasOne(e => e.GeneratedByUser)
                    .WithMany(u => u.GeneratedReports)
                    .HasForeignKey(e => e.GeneratedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}