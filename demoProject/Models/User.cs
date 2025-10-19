using System.ComponentModel.DataAnnotations;

namespace demoProject.Models
{
    public enum UserRole
    {
        Admin,
        Driver,
        Customer
    }

    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Shipment> SentShipments { get; set; } = new List<Shipment>();
        public ICollection<Shipment> AssignedShipments { get; set; } = new List<Shipment>();
        public ICollection<TrackingUpdate> TrackingUpdates { get; set; } = new List<TrackingUpdate>();
        public ICollection<Report> GeneratedReports { get; set; } = new List<Report>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}