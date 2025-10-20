using demoProject.Data;
using demoProject.Models;
using demoProject.DTOs;
using Microsoft.EntityFrameworkCore;

namespace demoProject.Services
{
    public interface IDriverAssignmentService
    {
        Task<List<DriverRecommendationResponse>> GetAvailableDriversForShipmentAsync(Guid shipmentId, AssignmentPriority priority = AssignmentPriority.Balanced);
        Task<DriverRecommendationResponse?> GetBestDriverForShipmentAsync(Guid shipmentId, AssignmentPriority priority = AssignmentPriority.Balanced);
        Task<bool> AssignDriverToShipmentAsync(Guid shipmentId, Guid driverId);
        Task UpdateDriverLocationAsync(Guid driverId, string address);
        Task UpdateDriverStatusAsync(Guid driverId, DriverStatus status);
        Task<List<DriverAvailabilityResponse>> GetAllDriversAvailabilityAsync();
        Task<bool> CreateDriverProfileAsync(Guid userId);
        Task<bool> UpdateDriverProfileAsync(Guid driverId, UpdateDriverProfileRequest request);
    }

    public class DriverAssignmentService : IDriverAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public DriverAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DriverRecommendationResponse>> GetAvailableDriversForShipmentAsync(Guid shipmentId, AssignmentPriority priority = AssignmentPriority.Balanced)
        {
            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == shipmentId);

            if (shipment == null)
                return new List<DriverRecommendationResponse>();

            // Get all available drivers - no coordinate requirements!
            var driversQuery = _context.Users
                .Where(u => u.Role == UserRole.Driver)
                .Include(u => u.AssignedShipments.Where(s => s.Status != ShipmentStatus.Delivered && 
                                                            s.Status != ShipmentStatus.Cancelled))
                .Join(_context.Drivers, u => u.Id, d => d.UserId, (u, d) => new { User = u, Driver = d })
                .Where(ud => ud.Driver.Status == DriverStatus.Available);

            var driversData = await driversQuery.ToListAsync();
            var recommendations = new List<DriverRecommendationResponse>();

            foreach (var driverData in driversData)
            {
                var driver = driverData.Driver;
                var user = driverData.User;

                // Check if driver has capacity for more shipments
                var activeShipments = user.AssignedShipments.Count;
                if (activeShipments >= driver.MaxActiveShipments)
                    continue;

                // Check if driver is within working hours (if specified)
                if (!IsWithinWorkingHours(driver))
                    continue;

                // Simplified region/area matching instead of GPS coordinates
                var regionMatch = CalculateRegionMatch(shipment, driver);

                var recommendation = new DriverRecommendationResponse
                {
                    Driver = new UserResponse
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.Role,
                        CreatedAt = user.CreatedAt
                    },
                    DriverDetails = new DriverDetailsResponse
                    {
                        Status = driver.Status,
                        CurrentAddress = driver.CurrentAddress,
                        MaxActiveShipments = driver.MaxActiveShipments,
                        VehicleType = driver.VehicleType,
                        LicenseNumber = driver.LicenseNumber,
                        IsVerified = driver.IsVerified,
                        LastActiveTime = driver.LastActiveTime,
                        WorkStartTime = driver.WorkStartTime,
                        WorkEndTime = driver.WorkEndTime,
                        PreferredRegion = driver.PreferredRegion
                    },
                    Distance = regionMatch, // Simple region matching score
                    ActiveShipments = activeShipments,
                    Rating = driver.Rating,
                    CompletedShipments = driver.CompletedShipments,
                    LastLocationUpdate = driver.LastLocationUpdate
                };

                // Calculate recommendation score based on priority (simplified)
                recommendation.Score = CalculateSimpleDriverScore(recommendation, priority);
                recommendation.RecommendationFactors = GetRecommendationFactors(recommendation, priority);
                recommendation.RecommendationReason = GetRecommendationReason(recommendation, priority);

                recommendations.Add(recommendation);
            }

            // Sort by score (highest first)
            return recommendations.OrderByDescending(r => r.Score).ToList();
        }

        public async Task<DriverRecommendationResponse?> GetBestDriverForShipmentAsync(Guid shipmentId, AssignmentPriority priority = AssignmentPriority.Balanced)
        {
            var recommendations = await GetAvailableDriversForShipmentAsync(shipmentId, priority);
            return recommendations.FirstOrDefault();
        }

        public async Task<bool> AssignDriverToShipmentAsync(Guid shipmentId, Guid driverId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            var user = await _context.Users
                .Include(u => u.AssignedShipments.Where(s => s.Status != ShipmentStatus.Delivered && 
                                                            s.Status != ShipmentStatus.Cancelled))
                .FirstOrDefaultAsync(u => u.Id == driverId && u.Role == UserRole.Driver);

            var driver = await _context.Drivers.FindAsync(driverId);

            if (shipment == null || user == null || driver == null)
                return false;

            // Check if driver is available and has capacity
            if (driver.Status != DriverStatus.Available || 
                user.AssignedShipments.Count >= driver.MaxActiveShipments)
                return false;

            shipment.AssignedDriverId = driverId;
            shipment.UpdatedAt = DateTime.UtcNow;

            // Update driver status if this is their first active shipment
            if (user.AssignedShipments.Count == 0)
            {
                driver.Status = DriverStatus.Busy;
            }

            driver.LastActiveTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateDriverLocationAsync(Guid driverId, string address)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                driver.CurrentAddress = address;
                driver.LastLocationUpdate = DateTime.UtcNow;
                driver.LastActiveTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateDriverStatusAsync(Guid driverId, DriverStatus status)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                driver.Status = status;
                driver.LastActiveTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<DriverAvailabilityResponse>> GetAllDriversAvailabilityAsync()
        {
            var driversData = await _context.Users
                .Where(u => u.Role == UserRole.Driver)
                .Include(u => u.AssignedShipments.Where(s => s.Status != ShipmentStatus.Delivered && 
                                                            s.Status != ShipmentStatus.Cancelled))
                .Join(_context.Drivers, u => u.Id, d => d.UserId, (u, d) => new { User = u, Driver = d })
                .ToListAsync();

            return driversData.Select(dd => new DriverAvailabilityResponse
            {
                Driver = new UserResponse
                {
                    Id = dd.User.Id,
                    FullName = dd.User.FullName,
                    Email = dd.User.Email,
                    Phone = dd.User.Phone,
                    Role = dd.User.Role,
                    CreatedAt = dd.User.CreatedAt
                },
                DriverDetails = new DriverDetailsResponse
                {
                    Status = dd.Driver.Status,
                    CurrentAddress = dd.Driver.CurrentAddress,
                    MaxActiveShipments = dd.Driver.MaxActiveShipments,
                    VehicleType = dd.Driver.VehicleType,
                    LicenseNumber = dd.Driver.LicenseNumber,
                    IsVerified = dd.Driver.IsVerified,
                    LastActiveTime = dd.Driver.LastActiveTime,
                    WorkStartTime = dd.Driver.WorkStartTime,
                    WorkEndTime = dd.Driver.WorkEndTime,
                    PreferredRegion = dd.Driver.PreferredRegion
                },
                ActiveShipments = dd.User.AssignedShipments.Count,
                IsAvailable = IsDriverAvailable(dd.Driver, dd.User.AssignedShipments.Count),
                AvailabilityReason = GetAvailabilityReason(dd.Driver, dd.User.AssignedShipments.Count)
            }).ToList();
        }

        public async Task<bool> CreateDriverProfileAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Role != UserRole.Driver)
                return false;

            var existingDriver = await _context.Drivers.FindAsync(userId);
            if (existingDriver != null)
                return false;

            var driver = new Driver
            {
                UserId = userId,
                Status = DriverStatus.Available,
                MaxActiveShipments = 5,
                Rating = 0,
                CompletedShipments = 0,
                TotalRatings = 0,
                IsVerified = false
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDriverProfileAsync(Guid driverId, UpdateDriverProfileRequest request)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
                return false;

            driver.MaxActiveShipments = request.MaxActiveShipments;
            driver.VehicleType = request.VehicleType;
            driver.LicenseNumber = request.LicenseNumber;
            driver.WorkStartTime = request.WorkStartTime;
            driver.WorkEndTime = request.WorkEndTime;
            driver.PreferredRegion = request.PreferredRegion;

            await _context.SaveChangesAsync();
            return true;
        }

        // SIMPLIFIED: Region/city-based matching instead of GPS coordinates
        private static double CalculateRegionMatch(Shipment shipment, Driver driver)
        {
            // Perfect match: same region
            if (!string.IsNullOrEmpty(shipment.OriginRegion) && 
                !string.IsNullOrEmpty(driver.PreferredRegion) &&
                shipment.OriginRegion.Equals(driver.PreferredRegion, StringComparison.OrdinalIgnoreCase))
            {
                return 0; // 0 = perfect match (like 0km distance)
            }

            // Good match: same city
            if (!string.IsNullOrEmpty(shipment.OriginCity) && 
                !string.IsNullOrEmpty(driver.CurrentAddress) &&
                driver.CurrentAddress.Contains(shipment.OriginCity, StringComparison.OrdinalIgnoreCase))
            {
                return 5; // 5 = good match (like 5km distance)
            }

            // Default: assume reasonable distance for same general area
            return 25; // 25 = acceptable match (like 25km distance)
        }

        private static double CalculateSimpleDriverScore(DriverRecommendationResponse recommendation, AssignmentPriority priority)
        {
            double score = 0;

            // Region match score (0=perfect, lower=better, normalized to 0-1)
            var regionScore = 1.0 - (recommendation.Distance / 50.0);
            regionScore = Math.Max(0, regionScore);

            switch (priority)
            {
                case AssignmentPriority.Distance:
                    score = regionScore * 0.8 + GetAvailabilityScore(recommendation) * 0.2;
                    break;

                case AssignmentPriority.Experience:
                    score = GetExperienceScore(recommendation.CompletedShipments) * 0.6 +
                           GetRatingScore(recommendation.Rating) * 0.3 +
                           GetAvailabilityScore(recommendation) * 0.1;
                    break;

                case AssignmentPriority.Rating:
                    score = GetRatingScore(recommendation.Rating) * 0.7 +
                           GetExperienceScore(recommendation.CompletedShipments) * 0.2 +
                           GetAvailabilityScore(recommendation) * 0.1;
                    break;

                case AssignmentPriority.Availability:
                    score = GetAvailabilityScore(recommendation) * 0.6 + regionScore * 0.4;
                    break;

                case AssignmentPriority.Balanced:
                default:
                    score = regionScore * 0.3 +
                           GetAvailabilityScore(recommendation) * 0.25 +
                           GetRatingScore(recommendation.Rating) * 0.25 +
                           GetExperienceScore(recommendation.CompletedShipments) * 0.2;
                    break;
            }

            // Apply simple bonuses/penalties
            if (recommendation.DriverDetails.IsVerified) score *= 1.1;
            if (recommendation.DriverDetails.LastActiveTime.HasValue &&
                DateTime.UtcNow - recommendation.DriverDetails.LastActiveTime.Value < TimeSpan.FromHours(2))
                score *= 1.05;

            return Math.Max(0, Math.Min(1, score));
        }

        // Removed GetDistanceScore - using region matching instead

        private static double GetAvailabilityScore(DriverRecommendationResponse recommendation)
        {
            var capacity = recommendation.DriverDetails.MaxActiveShipments;
            return (double)(capacity - recommendation.ActiveShipments) / capacity;
        }

        private static double GetRatingScore(decimal rating)
        {
            return (double)rating / 5.0;
        }

        private static double GetExperienceScore(int completedShipments)
        {
            return Math.Min(1.0, completedShipments / 100.0);
        }

        // Removed ApplyScoreModifiers - simplified scoring in main function

        private static List<string> GetRecommendationFactors(DriverRecommendationResponse recommendation, AssignmentPriority priority)
        {
            var factors = new List<string>();

            if (recommendation.Distance < 5)
                factors.Add("Same region/city");
            else if (recommendation.Distance < 15)
                factors.Add("Nearby area");

            if (recommendation.ActiveShipments == 0)
                factors.Add("Completely available");
            else if (recommendation.ActiveShipments < recommendation.DriverDetails.MaxActiveShipments / 2)
                factors.Add("Low workload");

            if (recommendation.Rating >= 4.5m)
                factors.Add("Excellent rating");
            else if (recommendation.Rating >= 4.0m)
                factors.Add("Good rating");

            if (recommendation.CompletedShipments > 50)
                factors.Add("Highly experienced");
            else if (recommendation.CompletedShipments > 20)
                factors.Add("Experienced");

            if (recommendation.DriverDetails.IsVerified)
                factors.Add("Verified driver");

            return factors;
        }

        private static string GetRecommendationReason(DriverRecommendationResponse recommendation, AssignmentPriority priority)
        {
            var factors = GetRecommendationFactors(recommendation, priority);
            if (!factors.Any())
                return "Available driver";

            return string.Join(", ", factors.Take(3));
        }

        private static bool IsWithinWorkingHours(Driver driver)
        {
            if (!driver.WorkStartTime.HasValue || !driver.WorkEndTime.HasValue)
                return true; // No restrictions if no working hours set

            var currentTime = TimeOnly.FromDateTime(DateTime.Now);
            var startTime = driver.WorkStartTime.Value;
            var endTime = driver.WorkEndTime.Value;

            if (startTime <= endTime)
            {
                // Same day working hours (e.g., 9 AM to 5 PM)
                return currentTime >= startTime && currentTime <= endTime;
            }
            else
            {
                // Overnight working hours (e.g., 10 PM to 6 AM)
                return currentTime >= startTime || currentTime <= endTime;
            }
        }

        private static bool IsDriverAvailable(Driver driver, int activeShipments)
        {
            return driver.Status == DriverStatus.Available &&
                   activeShipments < driver.MaxActiveShipments &&
                   IsWithinWorkingHours(driver);
        }

        private static string GetAvailabilityReason(Driver driver, int activeShipments)
        {
            if (driver.Status != DriverStatus.Available)
                return $"Status: {driver.Status}";

            if (activeShipments >= driver.MaxActiveShipments)
                return "At maximum capacity";

            if (!IsWithinWorkingHours(driver))
                return "Outside working hours";

            return "Available";
        }
    }
}