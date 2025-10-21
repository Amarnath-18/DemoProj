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
        Task<int> GetActiveShipmentsCountAsync(Guid driverId);
     Task<bool> RateDriverAsync(Guid driverId, Guid customerId, RateDriverRequest request);
     Task<bool> UpdateDriverVerificationAsync(Guid driverId, bool isVerified);
      Task<DriverRatingResponse?> GetDriverRatingAsync(Guid driverId);
        Task<ShipmentRatingStatusResponse?> CheckShipmentRatingStatusAsync(Guid shipmentId, Guid? userId = null);
    }

 public class DriverAssignmentService : IDriverAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistanceService _distanceService;
        private readonly ILogger<DriverAssignmentService> _logger;

        // Distance thresholds for smart recommendation
        private const double MAX_ACCEPTABLE_DISTANCE = 50.0; // km - Hard limit, reject drivers beyond this
        private const double PREFERRED_DISTANCE = 15.0; // km - Preferred maximum distance
        private const double EXCELLENT_DISTANCE = 5.0; // km - Excellent distance threshold

        public DriverAssignmentService(
    ApplicationDbContext context, 
IDistanceService distanceService,
      ILogger<DriverAssignmentService> logger)
        {
            _context = context;
            _distanceService = distanceService;
   _logger = logger;
        }

        public async Task<List<DriverRecommendationResponse>> GetAvailableDriversForShipmentAsync(Guid shipmentId, AssignmentPriority priority = AssignmentPriority.Balanced)
      {
            var shipment = await _context.Shipments
      .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
             return new List<DriverRecommendationResponse>();

       // Get all available drivers
       var driversQuery = _context.Users
   .Where(u => u.Role == UserRole.Driver)
    .Include(u => u.AssignedShipments.Where(s => s.Status != ShipmentStatus.Delivered && 
             s.Status != ShipmentStatus.Cancelled))
                .Join(_context.Drivers, u => u.Id, d => d.UserId, (u, d) => new { User = u, Driver = d })
        .Where(ud => ud.Driver.Status == DriverStatus.Available);

            var driversData = await driversQuery.ToListAsync();
          var recommendations = new List<DriverRecommendationResponse>();

            _logger.LogInformation("Found {DriverCount} available drivers for shipment {ShipmentId}", 
     driversData.Count, shipmentId);

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

      // Calculate actual distance using OpenStreetMap
      var distance = await CalculateActualDistanceAsync(shipment, driver);

                // SMART RECOMMENDATION: Apply distance-first filtering
  // Reject drivers beyond maximum acceptable distance
         if (distance > MAX_ACCEPTABLE_DISTANCE)
        {
            _logger.LogDebug("Driver {DriverId} rejected due to excessive distance: {Distance}km > {MaxDistance}km", 
       driver.UserId, distance, MAX_ACCEPTABLE_DISTANCE);
         continue;
        }

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
               Distance = distance, // Real distance in kilometers
       ActiveShipments = activeShipments,
             Rating = driver.Rating,
    CompletedShipments = driver.CompletedShipments,
       LastLocationUpdate = driver.LastLocationUpdate
      };

 // Calculate recommendation score with smart distance-first priority
            recommendation.Score = CalculateSmartDriverScore(recommendation, priority);
         recommendation.RecommendationFactors = GetRecommendationFactors(recommendation, priority);
      recommendation.RecommendationReason = GetRecommendationReason(recommendation, priority);

  recommendations.Add(recommendation);
    }

   _logger.LogInformation("Generated {RecommendationCount} driver recommendations for shipment {ShipmentId} after distance filtering", 
            recommendations.Count, shipmentId);

            // Smart sorting: First by distance tier, then by score within each tier
            return recommendations
       .OrderBy(r => GetDistanceTier(r.Distance))  // Primary sort: distance tier
                .ThenByDescending(r => r.Score)  // Secondary sort: score within tier
 .ToList();
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

   var responses = new List<DriverAvailabilityResponse>();
      
    foreach (var dd in driversData)
          {
 var response = new DriverAvailabilityResponse
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
          AvailabilityReason = GetAvailabilityReason(dd.Driver, dd.User.AssignedShipments.Count),
     
        // Rating details
            Rating = dd.Driver.Rating,
   CompletedShipments = dd.Driver.CompletedShipments,
       TotalRatings = dd.Driver.TotalRatings,
   LastLocationUpdate = dd.Driver.LastLocationUpdate,
    
 // Performance metrics
          PerformanceCategory = GetDriverPerformanceCategory(dd.Driver),
         PerformanceFactors = GetDriverPerformanceFactors(dd.Driver, dd.User.AssignedShipments.Count)
         };
  
       responses.Add(response);
      }

         // Sort by performance: Available first, then by rating and experience
            return responses
      .OrderByDescending(r => r.IsAvailable)
      .ThenByDescending(r => r.Rating)
.ThenByDescending(r => r.CompletedShipments)
      .ThenBy(r => r.ActiveShipments)
    .ToList();
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

        public async Task<int> GetActiveShipmentsCountAsync(Guid driverId)
        {
        return await _context.Shipments
   .Where(s => s.AssignedDriverId == driverId && 
         s.Status != ShipmentStatus.Delivered && 
       s.Status != ShipmentStatus.Cancelled)
                .CountAsync();
        }

        public async Task<bool> RateDriverAsync(Guid driverId, Guid customerId, RateDriverRequest request)
      {
        // Verify that the driver exists
            var driver = await _context.Drivers
          .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == driverId);
        
   if (driver == null)
  return false;

            // Verify that the shipment exists and the customer was the sender
       var shipment = await _context.Shipments
       .FirstOrDefaultAsync(s => s.Id == request.ShipmentId && 
        s.SenderId == customerId &&
    s.AssignedDriverId == driverId &&
          s.Status == ShipmentStatus.Delivered);
          
          if (shipment == null)
     return false;

     // Check if the customer has already rated this driver for this shipment
  var existingRating = await _context.DriverRatings
   .FirstOrDefaultAsync(dr => dr.CustomerId == customerId && 
    dr.ShipmentId == request.ShipmentId);
    
   if (existingRating != null)
         return false; // Already rated

        // Create new rating
         var driverRating = new DriverRating
 {
    DriverId = driverId,
    CustomerId = customerId,
  ShipmentId = request.ShipmentId,
         Rating = request.Rating,
        Comment = request.Comment,
         CreatedAt = DateTime.UtcNow
     };

          _context.DriverRatings.Add(driverRating);

            // Update driver's aggregated rating
    await UpdateDriverAggregatedRating(driverId);

          await _context.SaveChangesAsync();
            return true;
        }

    public async Task<bool> UpdateDriverVerificationAsync(Guid driverId, bool isVerified)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
        return false;

       driver.IsVerified = isVerified;
   await _context.SaveChangesAsync();
            return true;
        }

   public async Task<DriverRatingResponse?> GetDriverRatingAsync(Guid driverId)
        {
    var driver = await _context.Drivers
    .Include(d => d.User)
      .FirstOrDefaultAsync(d => d.UserId == driverId);
            
      if (driver == null)
         return null;

            // Get recent ratings with details
    var recentRatings = await _context.DriverRatings
         .Where(dr => dr.DriverId == driverId)
        .Include(dr => dr.Customer)
              .Include(dr => dr.Shipment)
.OrderByDescending(dr => dr.CreatedAt)
         .Take(10)
          .Select(dr => new DriverRatingDetail
        {
       Rating = dr.Rating,
              Comment = dr.Comment,
   RatedAt = dr.CreatedAt,
        RatedByCustomer = dr.Customer.FullName,
       ShipmentTrackingNumber = dr.Shipment.TrackingNumber
                })
        .ToListAsync();

    return new DriverRatingResponse
            {
        DriverId = driverId,
    Driver = new UserResponse
           {
  Id = driver.User.Id,
     FullName = driver.User.FullName,
  Email = driver.User.Email,
       Phone = driver.User.Phone,
     Role = driver.User.Role,
    CreatedAt = driver.User.CreatedAt
  },
  AverageRating = driver.Rating,
      TotalRatings = driver.TotalRatings,
       CompletedShipments = driver.CompletedShipments,
          IsVerified = driver.IsVerified,
     RecentRatings = recentRatings
     };
      }

     public async Task<ShipmentRatingStatusResponse?> CheckShipmentRatingStatusAsync(Guid shipmentId, Guid? userId = null)
        {
            // Get shipment with all related data
  var shipment = await _context.Shipments
      .Include(s => s.AssignedDriver)
              .Include(s => s.Sender)
    .FirstOrDefaultAsync(s => s.Id == shipmentId);

            if (shipment == null)
                return null;

      var response = new ShipmentRatingStatusResponse
   {
            ShipmentId = shipmentId,
        TrackingNumber = shipment.TrackingNumber,
    Driver = shipment.AssignedDriver != null ? new UserResponse
      {
      Id = shipment.AssignedDriver.Id,
  FullName = shipment.AssignedDriver.FullName,
    Email = shipment.AssignedDriver.Email,
Phone = shipment.AssignedDriver.Phone,
 Role = shipment.AssignedDriver.Role,
     CreatedAt = shipment.AssignedDriver.CreatedAt
     } : null
   };

            // Check if shipment has been rated by any user
 var anyRating = await _context.DriverRatings
 .Include(dr => dr.Customer)
      .FirstOrDefaultAsync(dr => dr.ShipmentId == shipmentId);

     if (anyRating != null)
   {
      response.IsRated = true;
 response.ExistingRating = new DriverRatingDetail
            {
 Rating = anyRating.Rating,
          Comment = anyRating.Comment,
           RatedAt = anyRating.CreatedAt,
       RatedByCustomer = anyRating.Customer.FullName,
    ShipmentTrackingNumber = shipment.TrackingNumber
     };
     }
    else
  {
                response.IsRated = false;
          }

     // Determine if the shipment can be rated (considering specific user if provided)
 if (userId.HasValue)
            {
    // Check if specific user can rate this shipment
    if (shipment.AssignedDriverId == null)
          {
      response.CanBeRated = false;
 response.RatingIneligibilityReason = "No driver assigned to this shipment";
  }
      else if (shipment.Status != ShipmentStatus.Delivered)
           {
   response.CanBeRated = false;
           response.RatingIneligibilityReason = "Shipment must be delivered before rating";
          }
      else if (shipment.SenderId != userId.Value)
          {
         response.CanBeRated = false;
  response.RatingIneligibilityReason = "Only the sender can rate the driver for this shipment";
           }
            else if (response.IsRated)
     {
          response.CanBeRated = false;
          response.RatingIneligibilityReason = "This shipment has already been rated";
      }
          else
       {
        response.CanBeRated = true;
             }
}
            else
  {
              // General eligibility without specific user context
       if (shipment.AssignedDriverId == null)
                {
       response.CanBeRated = false;
   response.RatingIneligibilityReason = "No driver assigned to this shipment";
   }
             else if (shipment.Status != ShipmentStatus.Delivered)
     {
       response.CanBeRated = false;
       response.RatingIneligibilityReason = "Shipment must be delivered before rating";
                }
  else if (response.IsRated)
        {
          response.CanBeRated = false;
           response.RatingIneligibilityReason = "This shipment has already been rated";
                }
 else
          {
       response.CanBeRated = true;
          }
        }

    return response;
        }

     /// <summary>
 /// Calculate actual distance between shipment origin and driver location using OpenStreetMap
        /// </summary>
  private async Task<double> CalculateActualDistanceAsync(Shipment shipment, Driver driver)
        {
 try
          {
      // If driver has no current address, use fallback region matching
   if (string.IsNullOrWhiteSpace(driver.CurrentAddress))
            {
                    _logger.LogWarning("Driver {DriverId} has no current address, using region matching fallback", 
             driver.UserId);
     return CalculateRegionMatchFallback(shipment, driver);
 }

 _logger.LogDebug("Calculating distance between shipment origin '{Origin}' and driver location '{DriverLocation}'", 
            shipment.OriginAddress, driver.CurrentAddress);

         // Use the distance service to calculate real distance
   var distance = await _distanceService.CalculateDistanceAsync(
         shipment.OriginAddress, 
        driver.CurrentAddress);

         _logger.LogDebug("Calculated distance: {Distance} km for driver {DriverId}", 
       distance, driver.UserId);

      return distance;
        }
            catch (Exception ex)
            {
   _logger.LogError(ex, "Error calculating distance for driver {DriverId}, using fallback", 
         driver.UserId);
       
      // Fallback to region matching if distance service fails
      return CalculateRegionMatchFallback(shipment, driver);
       }
     }

        /// <summary>
        /// Fallback region matching when geocoding fails or driver has no address
        /// </summary>
  private static double CalculateRegionMatchFallback(Shipment shipment, Driver driver)
        {
       // Perfect match: same region
   if (!string.IsNullOrEmpty(shipment.OriginRegion) && 
         !string.IsNullOrEmpty(driver.PreferredRegion) &&
            shipment.OriginRegion.Equals(driver.PreferredRegion, StringComparison.OrdinalIgnoreCase))
            {
    return 5; // 5km for same region
            }

   // Good match: same city
     if (!string.IsNullOrEmpty(shipment.OriginCity) && 
       !string.IsNullOrEmpty(driver.CurrentAddress) &&
   driver.CurrentAddress.Contains(shipment.OriginCity, StringComparison.OrdinalIgnoreCase))
          {
     return 10; // 10km for same city
            }

            // Default: assume reasonable distance for same general area
            return 25; // 25km for acceptable match
     }

        /// <summary>
   /// Smart driver scoring with distance-first priority
        /// </summary>
        private double CalculateSmartDriverScore(DriverRecommendationResponse recommendation, AssignmentPriority priority)
   {
            double score = 0;
  var distance = recommendation.Distance;

    // Distance score with exponential decay for farther distances
            var distanceScore = distance switch
  {
      <= EXCELLENT_DISTANCE => 1.0,      // 0-5km: Excellent
       <= PREFERRED_DISTANCE => 0.85,     // 5-15km: Very good  
       <= 25 => 0.65,                 // 15-25km: Good
          <= 35 => 0.45,          // 25-35km: Acceptable
 <= MAX_ACCEPTABLE_DISTANCE => 0.25, // 35-50km: Poor but acceptable
    _ => 0.1          // 50+km: Very poor (should be filtered out)
      };

      // Rating score (0-1)
         var ratingScore = GetRatingScore(recommendation.Rating);
            
            // Experience score (0-1) 
        var experienceScore = GetExperienceScore(recommendation.CompletedShipments);
            
            // Availability score (0-1)
            var availabilityScore = GetAvailabilityScore(recommendation);

         switch (priority)
            {
             case AssignmentPriority.Distance:
 // Maximum distance priority
     score = distanceScore * 0.9 + availabilityScore * 0.1;
      break;

                case AssignmentPriority.Experience:
          // Experience priority, but distance still important
         score = distanceScore * 0.4 + experienceScore * 0.4 + ratingScore * 0.15 + availabilityScore * 0.05;
             break;

      case AssignmentPriority.Rating:
    // Rating priority, but distance still important  
          score = distanceScore * 0.4 + ratingScore * 0.45 + experienceScore * 0.1 + availabilityScore * 0.05;
        break;

     case AssignmentPriority.Availability:
   // Availability priority, but distance remains important
           score = distanceScore * 0.5 + availabilityScore * 0.35 + ratingScore * 0.1 + experienceScore * 0.05;
        break;

  case AssignmentPriority.Balanced:
        default:
  // Balanced approach with distance as primary factor
    score = distanceScore * 0.45 +      // Distance remains most important
          ratingScore * 0.25 + // Rating second priority  
         availabilityScore * 0.18 +  // Availability third
          experienceScore * 0.12;     // Experience fourth
       break;
       }

      // Apply bonuses for exceptional drivers
            if (recommendation.DriverDetails.IsVerified) 
         score *= 1.08; // 8% bonus for verified drivers

            if (recommendation.DriverDetails.LastActiveTime.HasValue &&
  DateTime.UtcNow - recommendation.DriverDetails.LastActiveTime.Value < TimeSpan.FromHours(2))
   score *= 1.05; // 5% bonus for recently active drivers

       // Additional bonus for excellent distance + high rating combination
            if (distance <= EXCELLENT_DISTANCE && recommendation.Rating >= 4.5m)
              score *= 1.1; // 10% bonus for close + highly rated drivers

     return Math.Max(0, Math.Min(1, score));
        }

        /// <summary>
  /// Get distance tier for primary sorting (0 = best, higher = worse)
/// </summary>
    private int GetDistanceTier(double distance)
        {
    return distance switch
       {
        <= EXCELLENT_DISTANCE => 0,         // Tier 0: Excellent (0-5km)
      <= PREFERRED_DISTANCE => 1,   // Tier 1: Very good (5-15km)  
 <= 25 => 2,          // Tier 2: Good (15-25km)
    <= 35 => 3,          // Tier 3: Acceptable (25-35km)
           <= MAX_ACCEPTABLE_DISTANCE => 4,    // Tier 4: Poor (35-50km)
    _ => 5          // Tier 5: Very poor (50+km)
};
}

  private List<string> GetRecommendationFactors(DriverRecommendationResponse recommendation, AssignmentPriority priority)
 {
       var factors = new List<string>();

 // Enhanced distance-based factors with smart categorization
     if (recommendation.Distance <= EXCELLENT_DISTANCE)
    factors.Add($"Excellent proximity ({recommendation.Distance:F1}km)");
    else if (recommendation.Distance <= PREFERRED_DISTANCE)
            factors.Add($"Very close ({recommendation.Distance:F1}km)");
    else if (recommendation.Distance <= 25)
   factors.Add($"Good distance ({recommendation.Distance:F1}km)");
            else if (recommendation.Distance <= 35)
     factors.Add($"Acceptable distance ({recommendation.Distance:F1}km)");
 else if (recommendation.Distance <= MAX_ACCEPTABLE_DISTANCE)
    factors.Add($"Far but reachable ({recommendation.Distance:F1}km)");
     else
  factors.Add($"Very far ({recommendation.Distance:F1}km)");

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

          // Add special combinations
 if (recommendation.Distance <= EXCELLENT_DISTANCE && recommendation.Rating >= 4.5m)
    factors.Add("Premium choice");

         return factors;
   }

        private static string GetRecommendationReason(DriverRecommendationResponse recommendation, AssignmentPriority priority)
     {
        var factors = new List<string>();

        // Enhanced distance-based factors with smart categorization
            if (recommendation.Distance <= 5.0)
     factors.Add($"Excellent proximity ({recommendation.Distance:F1}km)");
        else if (recommendation.Distance <= 15.0)
   factors.Add($"Very close ({recommendation.Distance:F1}km)");
   else if (recommendation.Distance <= 25)
  factors.Add($"Good distance ({recommendation.Distance:F1}km)");
     else if (recommendation.Distance <= 35)
         factors.Add($"Acceptable distance ({recommendation.Distance:F1}km)");
   else if (recommendation.Distance <= 50.0)
       factors.Add($"Far but reachable ({recommendation.Distance:F1}km)");
            else
                factors.Add($"Very far ({recommendation.Distance:F1}km)");

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

       // Add special combinations
     if (recommendation.Distance <= 5.0 && recommendation.Rating >= 4.5m)
      factors.Add("Premium choice");

     if (!factors.Any())
                return "Available driver";

     return string.Join(", ", factors.Take(3));
        }

        private async Task UpdateDriverAggregatedRating(Guid driverId)
    {
         var driver = await _context.Drivers.FindAsync(driverId);
       if (driver == null)
    return;

  var ratings = await _context.DriverRatings
      .Where(dr => dr.DriverId == driverId)
        .ToListAsync();

  if (ratings.Any())
{
         driver.TotalRatings = ratings.Count;
      driver.Rating = (decimal)ratings.Average(r => r.Rating);
   }
            else
            {
      driver.TotalRatings = 0;
     driver.Rating = 0;
       }
        }

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

     /// <summary>
     /// Determine driver performance category based on rating and experience
      /// </summary>
 private static string GetDriverPerformanceCategory(Driver driver)
  {
            // Elite drivers: High rating + lots of experience
       if (driver.Rating >= 4.7m && driver.CompletedShipments >= 100)
     return "Elite";
  
  // Premium drivers: Very good rating + good experience
      if (driver.Rating >= 4.5m && driver.CompletedShipments >= 50)
      return "Premium";

    // Experienced drivers: Good rating + moderate experience
   if (driver.Rating >= 4.0m && driver.CompletedShipments >= 20)
     return "Experienced";
     
      // Good drivers: Decent rating + some experience
if (driver.Rating >= 3.5m && driver.CompletedShipments >= 10)
   return "Good";
    
 // New drivers: Limited experience but decent rating
      if (driver.CompletedShipments < 10 && driver.Rating >= 3.0m)
         return "New";
  
       // Needs improvement: Low rating or very limited experience
       if (driver.Rating < 3.0m || (driver.CompletedShipments == 0 && driver.TotalRatings == 0))
 return "Developing";
        
     return "Standard";
   }
        
/// <summary>
      /// Get performance factors for driver evaluation
    /// </summary>
      private static List<string> GetDriverPerformanceFactors(Driver driver, int activeShipments)
   {
       var factors = new List<string>();
        
       // Rating-based factors
    if (driver.Rating >= 4.7m)
    factors.Add("Exceptional rating");
         else if (driver.Rating >= 4.5m)
    factors.Add("Excellent rating");
    else if (driver.Rating >= 4.0m)
         factors.Add("Good rating");
     else if (driver.Rating >= 3.5m)
         factors.Add("Average rating");
     else if (driver.Rating > 0)
       factors.Add("Below average rating");
       else
factors.Add("No ratings yet");
   
      // Experience-based factors
     if (driver.CompletedShipments >= 100)
      factors.Add("Highly experienced");
else if (driver.CompletedShipments >= 50)
     factors.Add("Very experienced");
    else if (driver.CompletedShipments >= 20)
factors.Add("Experienced");
         else if (driver.CompletedShipments >= 5)
         factors.Add("Some experience");
     else if (driver.CompletedShipments > 0)
            factors.Add("Limited experience");
    else
       factors.Add("New driver");
         
         // Availability factors
   if (activeShipments == 0)
     factors.Add("Fully available");
       else if (activeShipments < driver.MaxActiveShipments / 2)
    factors.Add("Low workload");
 else if (activeShipments < driver.MaxActiveShipments)
    factors.Add("Moderate workload");
     else
    factors.Add("At capacity");
     
       // Verification status
     if (driver.IsVerified)
     factors.Add("Verified driver");
  else
        factors.Add("Unverified");
     
  // Activity factors
 if (driver.LastActiveTime.HasValue)
        {
var timeSinceActive = DateTime.UtcNow - driver.LastActiveTime.Value;
            if (timeSinceActive < TimeSpan.FromHours(1))
   factors.Add("Very active");
       else if (timeSinceActive < TimeSpan.FromHours(6))
         factors.Add("Recently active");
    else if (timeSinceActive < TimeSpan.FromDays(1))
 factors.Add("Active today");
       else if (timeSinceActive < TimeSpan.FromDays(7))
  factors.Add("Active this week");
        else
         factors.Add("Inactive");
       }
    else
   {
  factors.Add("No recent activity");
      }
  
    // Location update factors
    if (driver.LastLocationUpdate.HasValue)
  {
       var timeSinceUpdate = DateTime.UtcNow - driver.LastLocationUpdate.Value;
    if (timeSinceUpdate < TimeSpan.FromHours(1))
        factors.Add("Recent location");
       else if (timeSinceUpdate < TimeSpan.FromHours(6))
       factors.Add("Current location");
     else if (timeSinceUpdate < TimeSpan.FromDays(1))
      factors.Add("Location updated today");
      else
        factors.Add("Outdated location");
       }
      else
 {
      factors.Add("No location data");
     }
        
   return factors;
 }
    }
}