using demoProject.Data;
using demoProject.Models;
using Microsoft.EntityFrameworkCore;

namespace demoProject.Services
{
    public interface IDataSeeder
    {
        Task SeedAsync();
    }

    public class DataSeeder : IDataSeeder
    {
        private readonly ApplicationDbContext _context;

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Create sample users
            var admin = new User
            {
                FullName = "Admin User",
                Email = "admin@logistictracker.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Phone = "+1234567890",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var driver1 = new User
            {
                FullName = "John Driver",
                Email = "john.driver@logistictracker.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver123!"),
                Phone = "+1234567891",
                Role = UserRole.Driver,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var driver2 = new User
            {
                FullName = "Jane Driver",
                Email = "jane.driver@logistictracker.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver123!"),
                Phone = "+1234567892",
                Role = UserRole.Driver,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var customer1 = new User
            {
                FullName = "Alice Customer",
                Email = "alice.customer@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Phone = "+1234567893",
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var customer2 = new User
            {
                FullName = "Bob Customer",
                Email = "bob.customer@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Phone = "+1234567894",
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.AddRange(admin, driver1, driver2, customer1, customer2);
            await _context.SaveChangesAsync();

            // Create sample shipments
            var shipment1 = new Shipment
            {
                TrackingNumber = "LST123456",
                SenderId = customer1.Id,
                ReceiverName = "Charlie Receiver",
                ReceiverEmail = "charlie@example.com",
                ReceiverPhone = "+1234567895",
                OriginAddress = "123 Main St, New York, NY 10001",
                DestinationAddress = "456 Oak Ave, Los Angeles, CA 90001",
                OriginLatitude = 40.7128m,
                OriginLongitude = -74.0060m,
                DestinationLatitude = 34.0522m,
                DestinationLongitude = -118.2437m,
                Status = ShipmentStatus.Created,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var shipment2 = new Shipment
            {
                TrackingNumber = "LST789012",
                SenderId = customer2.Id,
                ReceiverName = "Diana Receiver",
                ReceiverEmail = "diana@example.com",
                ReceiverPhone = "+1234567896",
                OriginAddress = "789 Pine St, Chicago, IL 60601",
                DestinationAddress = "321 Elm St, Houston, TX 77001",
                OriginLatitude = 41.8781m,
                OriginLongitude = -87.6298m,
                DestinationLatitude = 29.7604m,
                DestinationLongitude = -95.3698m,
                Status = ShipmentStatus.PickedUp,
                AssignedDriverId = driver1.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            };

            var shipment3 = new Shipment
            {
                TrackingNumber = "LST345678",
                SenderId = customer1.Id,
                ReceiverName = "Eve Receiver",
                ReceiverEmail = "eve@example.com",
                ReceiverPhone = "+1234567897",
                OriginAddress = "555 Cedar Blvd, Phoenix, AZ 85001",
                DestinationAddress = "777 Maple Dr, Philadelphia, PA 19101",
                OriginLatitude = 33.4484m,
                OriginLongitude = -112.0740m,
                DestinationLatitude = 39.9526m,
                DestinationLongitude = -75.1652m,
                Status = ShipmentStatus.Delivered,
                AssignedDriverId = driver2.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddHours(-6)
            };

            _context.Shipments.AddRange(shipment1, shipment2, shipment3);
            await _context.SaveChangesAsync();

            // Create sample tracking updates
            var trackingUpdate1 = new TrackingUpdate
            {
                ShipmentId = shipment2.Id,
                Status = "Picked Up",
                Location = "Chicago Distribution Center",
                Latitude = 41.8781m,
                Longitude = -87.6298m,
                Remarks = "Package picked up from sender",
                UpdatedBy = driver1.Id,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            };

            var trackingUpdate2 = new TrackingUpdate
            {
                ShipmentId = shipment3.Id,
                Status = "Picked Up",
                Location = "Phoenix Distribution Center",
                Latitude = 33.4484m,
                Longitude = -112.0740m,
                Remarks = "Package picked up from sender",
                UpdatedBy = driver2.Id,
                Timestamp = DateTime.UtcNow.AddDays(-3)
            };

            var trackingUpdate3 = new TrackingUpdate
            {
                ShipmentId = shipment3.Id,
                Status = "In Transit",
                Location = "Dallas Transit Hub",
                Latitude = 32.7767m,
                Longitude = -96.7970m,
                Remarks = "Package in transit to destination",
                UpdatedBy = driver2.Id,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            };

            var trackingUpdate4 = new TrackingUpdate
            {
                ShipmentId = shipment3.Id,
                Status = "Delivered",
                Location = "777 Maple Dr, Philadelphia, PA 19101",
                Latitude = 39.9526m,
                Longitude = -75.1652m,
                Remarks = "Package delivered successfully",
                UpdatedBy = driver2.Id,
                Timestamp = DateTime.UtcNow.AddHours(-6)
            };

            _context.TrackingUpdates.AddRange(trackingUpdate1, trackingUpdate2, trackingUpdate3, trackingUpdate4);
            await _context.SaveChangesAsync();

            // Create sample notifications
            var notification1 = new Notification
            {
                ShipmentId = shipment2.Id,
                Type = NotificationType.Email,
                Recipient = "diana@example.com",
                Message = "Your shipment LST789012 has been picked up and is on its way.",
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow.AddHours(-2)
            };

            var notification2 = new Notification
            {
                ShipmentId = shipment3.Id,
                Type = NotificationType.Email,
                Recipient = "eve@example.com",
                Message = "Your shipment LST345678 has been delivered successfully.",
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow.AddHours(-6)
            };

            _context.Notifications.AddRange(notification1, notification2);
            await _context.SaveChangesAsync();
        }
    }
}