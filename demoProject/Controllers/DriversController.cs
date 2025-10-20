using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using demoProject.DTOs;
using demoProject.Services;
using System.Security.Claims;

namespace demoProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DriversController : ControllerBase
    {
        private readonly IDriverAssignmentService _driverAssignmentService;

        public DriversController(IDriverAssignmentService driverAssignmentService)
        {
            _driverAssignmentService = driverAssignmentService;
        }

        [HttpPost("location")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UpdateLocation(UpdateDriverLocationRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            await _driverAssignmentService.UpdateDriverLocationAsync(
                currentUserId, 
                request.Address);

            return NoContent();
        }

        [HttpPut("status")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UpdateStatus(UpdateDriverStatusRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            await _driverAssignmentService.UpdateDriverStatusAsync(currentUserId, request.Status);
            return NoContent();
        }

        [HttpPost("profile")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> CreateProfile()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            var success = await _driverAssignmentService.CreateDriverProfileAsync(currentUserId);
            if (!success)
            {
                return BadRequest("Driver profile already exists or user is not a driver");
            }

            return NoContent();
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UpdateProfile(UpdateDriverProfileRequest request)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return BadRequest("Invalid user ID");
            }

            var success = await _driverAssignmentService.UpdateDriverProfileAsync(currentUserId, request);
            if (!success)
            {
                return NotFound("Driver profile not found");
            }

            return NoContent();
        }

        [HttpGet("availability")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<DriverAvailabilityResponse>>> GetAllDriversAvailability()
        {
            var drivers = await _driverAssignmentService.GetAllDriversAvailabilityAsync();
            return Ok(drivers);
        }
    }
}