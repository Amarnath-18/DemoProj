using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using demoProject.Data;
using demoProject.DTOs;
using demoProject.Models;
using demoProject.Services;
using System.Security.Claims;

namespace demoProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShipmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ShipmentsController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShipmentResponse>>> GetShipments()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Shipment> query = _context.Shipments
                .Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                    .ThenInclude(tu => tu.UpdatedByUser);

            // Filter based on user role
            query = currentUserRole switch
            {
                "Admin" => query, // Admin can see all shipments
                "Driver" => query.Where(s => s.AssignedDriverId == currentUserId),
                "Customer" => query.Where(s => s.SenderId == currentUserId),
                _ => query.Where(s => false) // No access
            };

            var shipments = await query.Select(s => new ShipmentResponse
            {
                Id = s.Id,
                TrackingNumber = s.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = s.Sender.Id,
                    FullName = s.Sender.FullName,
                    Email = s.Sender.Email,
                    Phone = s.Sender.Phone,
                    Role = s.Sender.Role,
                    CreatedAt = s.Sender.CreatedAt
                },
                ReceiverName = s.ReceiverName,
                ReceiverEmail = s.ReceiverEmail,
                ReceiverPhone = s.ReceiverPhone,
                OriginAddress = s.OriginAddress,
                DestinationAddress = s.DestinationAddress,
                OriginLatitude = s.OriginLatitude,
                OriginLongitude = s.OriginLongitude,
                DestinationLatitude = s.DestinationLatitude,
                DestinationLongitude = s.DestinationLongitude,
                Status = s.Status,
                AssignedDriver = s.AssignedDriver != null ? new UserResponse
                {
                    Id = s.AssignedDriver.Id,
                    FullName = s.AssignedDriver.FullName,
                    Email = s.AssignedDriver.Email,
                    Phone = s.AssignedDriver.Phone,
                    Role = s.AssignedDriver.Role,
                    CreatedAt = s.AssignedDriver.CreatedAt
                } : null,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                TrackingUpdates = s.TrackingUpdates.Select(tu => new TrackingUpdateResponse
                {
                    Id = tu.Id,
                    Status = tu.Status,
                    Location = tu.Location,
                    Latitude = tu.Latitude,
                    Longitude = tu.Longitude,
                    Remarks = tu.Remarks,
                    UpdatedBy = new UserResponse
                    {
                        Id = tu.UpdatedByUser.Id,
                        FullName = tu.UpdatedByUser.FullName,
                        Email = tu.UpdatedByUser.Email,
                        Phone = tu.UpdatedByUser.Phone,
                        Role = tu.UpdatedByUser.Role,
                        CreatedAt = tu.UpdatedByUser.CreatedAt
                    },
                    Timestamp = tu.Timestamp
                }).ToList()
            }).ToListAsync();

            return Ok(shipments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ShipmentResponse>> GetShipment(Guid id)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var shipment = await _context.Shipments
                .Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                    .ThenInclude(tu => tu.UpdatedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
            {
                return NotFound();
            }

            // Check access permissions
            var hasAccess = currentUserRole switch
            {
                "Admin" => true,
                "Driver" => shipment.AssignedDriverId == currentUserId,
                "Customer" => shipment.SenderId == currentUserId,
                _ => false
            };

            if (!hasAccess)
            {
                return Forbid();
            }

            var response = new ShipmentResponse
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = shipment.Sender.Id,
                    FullName = shipment.Sender.FullName,
                    Email = shipment.Sender.Email,
                    Phone = shipment.Sender.Phone,
                    Role = shipment.Sender.Role,
                    CreatedAt = shipment.Sender.CreatedAt
                },
                ReceiverName = shipment.ReceiverName,
                ReceiverEmail = shipment.ReceiverEmail,
                ReceiverPhone = shipment.ReceiverPhone,
                OriginAddress = shipment.OriginAddress,
                DestinationAddress = shipment.DestinationAddress,
                OriginLatitude = shipment.OriginLatitude,
                OriginLongitude = shipment.OriginLongitude,
                DestinationLatitude = shipment.DestinationLatitude,
                DestinationLongitude = shipment.DestinationLongitude,
                Status = shipment.Status,
                AssignedDriver = shipment.AssignedDriver != null ? new UserResponse
                {
                    Id = shipment.AssignedDriver.Id,
                    FullName = shipment.AssignedDriver.FullName,
                    Email = shipment.AssignedDriver.Email,
                    Phone = shipment.AssignedDriver.Phone,
                    Role = shipment.AssignedDriver.Role,
                    CreatedAt = shipment.AssignedDriver.CreatedAt
                } : null,
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                TrackingUpdates = shipment.TrackingUpdates.Select(tu => new TrackingUpdateResponse
                {
                    Id = tu.Id,
                    Status = tu.Status,
                    Location = tu.Location,
                    Latitude = tu.Latitude,
                    Longitude = tu.Longitude,
                    Remarks = tu.Remarks,
                    UpdatedBy = new UserResponse
                    {
                        Id = tu.UpdatedByUser.Id,
                        FullName = tu.UpdatedByUser.FullName,
                        Email = tu.UpdatedByUser.Email,
                        Phone = tu.UpdatedByUser.Phone,
                        Role = tu.UpdatedByUser.Role,
                        CreatedAt = tu.UpdatedByUser.CreatedAt
                    },
                    Timestamp = tu.Timestamp
                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<ActionResult<ShipmentResponse>> CreateShipment(CreateShipmentRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            var shipment = new Shipment
            {
                TrackingNumber = GenerateTrackingNumber(),
                SenderId = currentUserId,
                ReceiverName = request.ReceiverName,
                ReceiverEmail = request.ReceiverEmail,
                ReceiverPhone = request.ReceiverPhone,
                OriginAddress = request.OriginAddress,
                DestinationAddress = request.DestinationAddress,
                OriginLatitude = request.OriginLatitude,
                OriginLongitude = request.OriginLongitude,
                DestinationLatitude = request.DestinationLatitude,
                DestinationLongitude = request.DestinationLongitude
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Load the shipment with related data
            shipment = await _context.Shipments
                .Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                .FirstAsync(s => s.Id == shipment.Id);

            // Send initial notification
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, "Created");

            var response = new ShipmentResponse
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = shipment.Sender.Id,
                    FullName = shipment.Sender.FullName,
                    Email = shipment.Sender.Email,
                    Phone = shipment.Sender.Phone,
                    Role = shipment.Sender.Role,
                    CreatedAt = shipment.Sender.CreatedAt
                },
                ReceiverName = shipment.ReceiverName,
                ReceiverEmail = shipment.ReceiverEmail,
                ReceiverPhone = shipment.ReceiverPhone,
                OriginAddress = shipment.OriginAddress,
                DestinationAddress = shipment.DestinationAddress,
                OriginLatitude = shipment.OriginLatitude,
                OriginLongitude = shipment.OriginLongitude,
                DestinationLatitude = shipment.DestinationLatitude,
                DestinationLongitude = shipment.DestinationLongitude,
                Status = shipment.Status,
                AssignedDriver = null,
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                TrackingUpdates = new List<TrackingUpdateResponse>()
            };

            return CreatedAtAction(nameof(GetShipment), new { id = shipment.Id }, response);
        }

        [HttpPut("{id}/assign-driver")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignDriver(Guid id, AssignDriverRequest request)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound();
            }

            var driver = await _context.Users.FindAsync(request.DriverId);
            if (driver == null || driver.Role != UserRole.Driver)
            {
                return BadRequest("Invalid driver");
            }

            shipment.AssignedDriverId = request.DriverId;
            shipment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UpdateShipmentStatus(Guid id, UpdateShipmentStatusRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            var shipment = await _context.Shipments
                .Include(s => s.AssignedDriver)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
            {
                return NotFound();
            }

            // Only assigned driver can update status
            if (shipment.AssignedDriverId != currentUserId)
            {
                return Forbid();
            }

            shipment.Status = request.Status;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Create tracking update
            var trackingUpdate = new TrackingUpdate
            {
                ShipmentId = id,
                Status = request.Status.ToString(),
                Location = request.Location,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Remarks = request.Remarks,
                UpdatedBy = currentUserId
            };

            _context.TrackingUpdates.Add(trackingUpdate);
            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, request.Status.ToString());

            return NoContent();
        }

        [HttpGet("track/{trackingNumber}")]
        [AllowAnonymous]
        public async Task<ActionResult<ShipmentResponse>> TrackShipment(string trackingNumber)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Sender)
                .Include(s => s.AssignedDriver)
                .Include(s => s.TrackingUpdates)
                    .ThenInclude(tu => tu.UpdatedByUser)
                .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);

            if (shipment == null)
            {
                return NotFound("Shipment not found");
            }

            var response = new ShipmentResponse
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                Sender = new UserResponse
                {
                    Id = shipment.Sender.Id,
                    FullName = shipment.Sender.FullName,
                    Email = shipment.Sender.Email,
                    Phone = shipment.Sender.Phone,
                    Role = shipment.Sender.Role,
                    CreatedAt = shipment.Sender.CreatedAt
                },
                ReceiverName = shipment.ReceiverName,
                ReceiverEmail = shipment.ReceiverEmail,
                ReceiverPhone = shipment.ReceiverPhone,
                OriginAddress = shipment.OriginAddress,
                DestinationAddress = shipment.DestinationAddress,
                OriginLatitude = shipment.OriginLatitude,
                OriginLongitude = shipment.OriginLongitude,
                DestinationLatitude = shipment.DestinationLatitude,
                DestinationLongitude = shipment.DestinationLongitude,
                Status = shipment.Status,
                AssignedDriver = shipment.AssignedDriver != null ? new UserResponse
                {
                    Id = shipment.AssignedDriver.Id,
                    FullName = shipment.AssignedDriver.FullName,
                    Email = shipment.AssignedDriver.Email,
                    Phone = shipment.AssignedDriver.Phone,
                    Role = shipment.AssignedDriver.Role,
                    CreatedAt = shipment.AssignedDriver.CreatedAt
                } : null,
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                TrackingUpdates = shipment.TrackingUpdates.Select(tu => new TrackingUpdateResponse
                {
                    Id = tu.Id,
                    Status = tu.Status,
                    Location = tu.Location,
                    Latitude = tu.Latitude,
                    Longitude = tu.Longitude,
                    Remarks = tu.Remarks,
                    UpdatedBy = new UserResponse
                    {
                        Id = tu.UpdatedByUser.Id,
                        FullName = tu.UpdatedByUser.FullName,
                        Email = tu.UpdatedByUser.Email,
                        Phone = tu.UpdatedByUser.Phone,
                        Role = tu.UpdatedByUser.Role,
                        CreatedAt = tu.UpdatedByUser.CreatedAt
                    },
                    Timestamp = tu.Timestamp
                }).OrderBy(tu => tu.Timestamp).ToList()
            };

            return Ok(response);
        }

        private static string GenerateTrackingNumber()
        {
            var random = new Random();
            var prefix = "LST";
            var number = random.Next(100000, 999999);
            return $"{prefix}{number}";
        }
    }
}