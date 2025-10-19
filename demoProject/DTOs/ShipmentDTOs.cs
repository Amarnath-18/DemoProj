using System.ComponentModel.DataAnnotations;
using demoProject.Models;

namespace demoProject.DTOs
{
    // Shipment DTOs
    public class CreateShipmentRequest
    {
        [Required]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ReceiverEmail { get; set; } = string.Empty;

        [Phone]
        public string? ReceiverPhone { get; set; }

        [Required]
        public string OriginAddress { get; set; } = string.Empty;

        [Required]
        public string DestinationAddress { get; set; } = string.Empty;

        public decimal? OriginLatitude { get; set; }
        public decimal? OriginLongitude { get; set; }
        public decimal? DestinationLatitude { get; set; }
        public decimal? DestinationLongitude { get; set; }
    }

    public class AssignDriverRequest
    {
        [Required]
        public Guid DriverId { get; set; }
    }

    public class UpdateShipmentStatusRequest
    {
        [Required]
        public ShipmentStatus Status { get; set; }

        public string? Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Remarks { get; set; }
    }

    public class ShipmentResponse
    {
        public Guid Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public UserResponse Sender { get; set; } = null!;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverEmail { get; set; } = string.Empty;
        public string? ReceiverPhone { get; set; }
        public string OriginAddress { get; set; } = string.Empty;
        public string DestinationAddress { get; set; } = string.Empty;
        public decimal? OriginLatitude { get; set; }
        public decimal? OriginLongitude { get; set; }
        public decimal? DestinationLatitude { get; set; }
        public decimal? DestinationLongitude { get; set; }
        public ShipmentStatus Status { get; set; }
        public UserResponse? AssignedDriver { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<TrackingUpdateResponse> TrackingUpdates { get; set; } = new();
    }

    public class TrackingUpdateResponse
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Remarks { get; set; }
        public UserResponse UpdatedBy { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}