using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace demoProject.Models
{
    public class TrackingUpdate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ShipmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Location { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        [StringLength(255)]
        public string? Remarks { get; set; }

        [Required]
        public Guid UpdatedBy { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ShipmentId")]
        public Shipment Shipment { get; set; } = null!;

        [ForeignKey("UpdatedBy")]
        public User UpdatedByUser { get; set; } = null!;
    }
}