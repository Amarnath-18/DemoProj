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
        private readonly IDriverAssignmentService _driverAssignmentService;

        public ShipmentsController(
            ApplicationDbContext context, 
            INotificationService notificationService,
            IDriverAssignmentService driverAssignmentService)
        {
            _context = context;
            _notificationService = notificationService;
            _driverAssignmentService = driverAssignmentService;
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
                OriginCity = s.OriginCity,
                OriginRegion = s.OriginRegion,
                DestinationCity = s.DestinationCity,
                DestinationRegion = s.DestinationRegion,
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
                OriginCity = shipment.OriginCity,
                OriginRegion = shipment.OriginRegion,
                DestinationCity = shipment.DestinationCity,
                DestinationRegion = shipment.DestinationRegion,
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
                OriginCity = request.OriginCity,
                OriginRegion = request.OriginRegion,
                DestinationCity = request.DestinationCity,
                DestinationRegion = request.DestinationRegion
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
                OriginCity = shipment.OriginCity,
                OriginRegion = shipment.OriginRegion,
                DestinationCity = shipment.DestinationCity,
                DestinationRegion = shipment.DestinationRegion,
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

        [HttpGet("{id}/driver-recommendations")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<DriverRecommendationResponse>>> GetDriverRecommendations(
            Guid id, 
            [FromQuery] AssignmentPriority priority = AssignmentPriority.Balanced)
        {
            var recommendations = await _driverAssignmentService.GetAvailableDriversForShipmentAsync(id, priority);
            return Ok(recommendations);
        }

        [HttpPost("{id}/smart-assign")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DriverRecommendationResponse>> SmartAssignDriver(
            Guid id, 
            SmartAssignDriverRequest request)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound("Shipment not found");
            }

            if (shipment.AssignedDriverId != null)
            {
                return BadRequest("Shipment is already assigned to a driver");
            }

            DriverRecommendationResponse? selectedRecommendation = null;

            if (request.PreferredDriverId.HasValue)
            {
                // Admin selected a specific driver
                var recommendations = await _driverAssignmentService.GetAvailableDriversForShipmentAsync(id, request.Priority);
                selectedRecommendation = recommendations.FirstOrDefault(r => r.Driver.Id == request.PreferredDriverId.Value);
                
                if (selectedRecommendation == null)
                {
                    return BadRequest("Selected driver is not available for this shipment");
                }
            }
            else if (request.UseAutoAssignment)
            {
                // Use best recommended driver
                selectedRecommendation = await _driverAssignmentService.GetBestDriverForShipmentAsync(id, request.Priority);
                
                if (selectedRecommendation == null)
                {
                    return NotFound("No available drivers found for this shipment");
                }
            }
            else
            {
                // Return recommendations without assigning
                var recommendations = await _driverAssignmentService.GetAvailableDriversForShipmentAsync(id, request.Priority);
                var limitedRecommendations = recommendations.Take(request.MaxRecommendations).ToList();
                
                return Ok(limitedRecommendations);
            }

            // Assign the driver
            var success = await _driverAssignmentService.AssignDriverToShipmentAsync(id, selectedRecommendation.Driver.Id);
            if (!success)
            {
                return BadRequest("Failed to assign driver to shipment");
            }

            // Send notification
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, "Driver Assigned");

            return Ok(selectedRecommendation);
        }

        [HttpGet("available-drivers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<DriverAvailabilityResponse>>> GetAvailableDrivers()
        {
            var drivers = await _driverAssignmentService.GetAllDriversAvailabilityAsync();
            return Ok(drivers);
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

            // Prevent updates once shipment is delivered or cancelled
            if (shipment.Status == ShipmentStatus.Delivered)
            {
                return BadRequest("Cannot update a shipment that has already been delivered");
            }

            if (shipment.Status == ShipmentStatus.Cancelled)
            {
                return BadRequest("Cannot update a shipment that has been cancelled");
            }

            shipment.Status = request.Status;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Create tracking update
            var trackingUpdate = new TrackingUpdate
            {
                ShipmentId = id,
                Status = request.Status.ToString(),
                Location = request.Location,
                Remarks = request.Remarks,
                UpdatedBy = currentUserId
            };

            _context.TrackingUpdates.Add(trackingUpdate);

            // Handle delivery completion logic
            if (request.Status == ShipmentStatus.Delivered)
            {
                await HandleShipmentDeliveryAsync(currentUserId);
            }

            // Handle cancellation logic
            if (request.Status == ShipmentStatus.Cancelled)
            {
                await HandleShipmentCancellationAsync(currentUserId);
            }

            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, request.Status.ToString());

            return NoContent();
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> CancelShipment(Guid id, CancelShipmentRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var shipment = await _context.Shipments
                .Include(s => s.AssignedDriver)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
            {
                return NotFound();
            }

            // Check permissions - customers can only cancel their own shipments
            if (currentUserRole == "Customer" && shipment.SenderId != currentUserId)
            {
                return Forbid();
            }

            // Prevent cancelling already delivered or cancelled shipments
            if (shipment.Status == ShipmentStatus.Delivered)
            {
                return BadRequest("Cannot cancel a shipment that has already been delivered");
            }

            if (shipment.Status == ShipmentStatus.Cancelled)
            {
                return BadRequest("Shipment is already cancelled");
            }

            shipment.Status = ShipmentStatus.Cancelled;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Create tracking update
            var trackingUpdate = new TrackingUpdate
            {
                ShipmentId = id,
                Status = ShipmentStatus.Cancelled.ToString(),
                Location = request.Location ?? "System",
                Remarks = request.Reason ?? "Shipment cancelled",
                UpdatedBy = currentUserId
            };

            _context.TrackingUpdates.Add(trackingUpdate);

            // Handle cancellation logic if shipment was assigned to a driver
            if (shipment.AssignedDriverId.HasValue)
            {
                await HandleShipmentCancellationAsync(shipment.AssignedDriverId.Value);
            }

            await _context.SaveChangesAsync();

            // Send notification
            await _notificationService.NotifyShipmentStatusChangeAsync(shipment, "Cancelled");

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
                OriginCity = shipment.OriginCity,
                OriginRegion = shipment.OriginRegion,
                DestinationCity = shipment.DestinationCity,
                DestinationRegion = shipment.DestinationRegion,
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

        private async Task HandleShipmentDeliveryAsync(Guid driverId)
        {
            // Get the driver profile
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null) return;

            // Increment completed shipments count
            driver.CompletedShipments++;
            driver.LastActiveTime = DateTime.UtcNow;

            // Check remaining active shipments for this driver
            var activeShipmentsCount = await _context.Shipments
                .Where(s => s.AssignedDriverId == driverId && 
                           s.Status != ShipmentStatus.Delivered && 
                           s.Status != ShipmentStatus.Cancelled)
                .CountAsync();

            // Update driver status based on remaining workload
            if (activeShipmentsCount == 0)
            {
                // No more active shipments - driver becomes available
                driver.Status = DriverStatus.Available;
            }
            else if (activeShipmentsCount < driver.MaxActiveShipments)
            {
                // Still has capacity - ensure driver is available for new assignments
                if (driver.Status == DriverStatus.Busy)
                {
                    driver.Status = DriverStatus.Available;
                }
            }
            // If still at max capacity, keep current status
        }

        private async Task HandleShipmentCancellationAsync(Guid driverId)
        {
            // Get the driver profile
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null) return;

            // Update last active time
            driver.LastActiveTime = DateTime.UtcNow;

            // Check remaining active shipments for this driver
            var activeShipmentsCount = await _context.Shipments
                .Where(s => s.AssignedDriverId == driverId && 
                           s.Status != ShipmentStatus.Delivered && 
                           s.Status != ShipmentStatus.Cancelled)
                .CountAsync();

            // Update driver status based on remaining workload
            if (activeShipmentsCount == 0)
            {
                // No more active shipments - driver becomes available
                driver.Status = DriverStatus.Available;
            }
            else if (activeShipmentsCount < driver.MaxActiveShipments)
            {
                // Still has capacity - ensure driver is available for new assignments
                if (driver.Status == DriverStatus.Busy)
                {
                    driver.Status = DriverStatus.Available;
                }
            }
            // If still at max capacity, keep current status
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