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
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<TrackingUpdate> TrackingUpdates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DriverRating> DriverRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure all DateTime properties to use UTC
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }

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
                    .OnDelete(DeleteBehavior.Cascade);

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

            // Driver configuration
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<Driver>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DriverRating configuration
            modelBuilder.Entity<DriverRating>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Ensure one rating per customer per shipment
                entity.HasIndex(e => new { e.CustomerId, e.ShipmentId }).IsUnique();

                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Shipment)
                    .WithMany()
                    .HasForeignKey(e => e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}