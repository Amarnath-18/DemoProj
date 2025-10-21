using SendGrid;
using SendGrid.Helpers.Mail;
using demoProject.Models;
using demoProject.Data;

namespace demoProject.Services
{
    public interface INotificationService
    {
        Task SendEmailNotificationAsync(string email, string subject, string message);
        Task SendSMSNotificationAsync(string phoneNumber, string message);
        Task NotifyShipmentStatusChangeAsync(Shipment shipment, string status);
    }

    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly SmsService _smsService;

        public NotificationService(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<NotificationService> logger,
            SmsService smsService)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _smsService = smsService;
        }

        public async Task SendEmailNotificationAsync(string email, string subject, string message)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("SendGrid API key not configured");
                    return;
                }

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(_configuration["SendGrid:FromEmail"], "Logistic Tracker");
                var to = new EmailAddress(email);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

                var response = await client.SendEmailAsync(msg);
                _logger.LogInformation($"Email sent to {email}, Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
            }
        }

        public async Task SendSMSNotificationAsync(string phoneNumber, string message)
        {
            try
            {
                await _smsService.SendSmsAsync(phoneNumber, message);
                _logger.LogInformation($"SMS sent to {phoneNumber}: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
            }
        }

        public async Task NotifyShipmentStatusChangeAsync(Shipment shipment, string status)
        {
            try
            {
                var subject = $"Shipment Update - {shipment.TrackingNumber}";
                var message = $"Your shipment {shipment.TrackingNumber} status has been updated to: {status}";

                // Send email to receiver
                await SendEmailNotificationAsync(shipment.ReceiverEmail, subject, message);

                // Send SMS if phone number is available
                if (!string.IsNullOrEmpty(shipment.ReceiverPhone))
                {
                    await SendSMSNotificationAsync(shipment.ReceiverPhone, message);
                }

                // Save notification to database
                var emailNotification = new Notification
                {
                    ShipmentId = shipment.Id,
                    Type = NotificationType.Email,
                    Recipient = shipment.ReceiverEmail,
                    Message = message,
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow
                };

                _context.Notifications.Add(emailNotification);

                if (!string.IsNullOrEmpty(shipment.ReceiverPhone))
                {
                    var smsNotification = new Notification
                    {
                        ShipmentId = shipment.Id,
                        Type = NotificationType.SMS,
                        Recipient = shipment.ReceiverPhone,
                        Message = message,
                        Status = NotificationStatus.Sent,
                        SentAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(smsNotification);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notifications for shipment {shipment.Id}");
            }
        }
    }
}