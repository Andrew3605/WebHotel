using System.Net;
using System.Net.Mail;

namespace WebHotel.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 25;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool EnableSsl { get; set; }
        public string FromAddress { get; set; } = "noreply@webhotel.local";
        public string FromName { get; set; } = "WebHotel";
    }

    public class SmtpEmailSender : IHotelEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(SmtpSettings settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(string toEmail, string customerName,
            int bookingId, string roomNumber, DateTime checkIn, DateTime checkOut, decimal totalPrice)
        {
            var subject = $"Booking Confirmation - #{bookingId}";
            var body = $"""
                <h2>Booking Confirmed</h2>
                <p>Hi {Encode(customerName)},</p>
                <p>Your booking has been confirmed. Here are the details:</p>
                <table style="border-collapse:collapse;">
                    <tr><td style="padding:4px 12px;font-weight:bold;">Booking ID</td><td style="padding:4px 12px;">#{bookingId}</td></tr>
                    <tr><td style="padding:4px 12px;font-weight:bold;">Room</td><td style="padding:4px 12px;">{Encode(roomNumber)}</td></tr>
                    <tr><td style="padding:4px 12px;font-weight:bold;">Check-in</td><td style="padding:4px 12px;">{checkIn:dd MMM yyyy}</td></tr>
                    <tr><td style="padding:4px 12px;font-weight:bold;">Check-out</td><td style="padding:4px 12px;">{checkOut:dd MMM yyyy}</td></tr>
                    <tr><td style="padding:4px 12px;font-weight:bold;">Total</td><td style="padding:4px 12px;">{totalPrice:C}</td></tr>
                </table>
                <p>Thank you for choosing WebHotel!</p>
                """;

            await SendAsync(toEmail, subject, body);
        }

        public async Task SendPaymentReceiptAsync(string toEmail, string customerName,
            int bookingId, decimal amount, decimal balanceDue)
        {
            var subject = $"Payment Receipt - Booking #{bookingId}";
            var body = $"""
                <h2>Payment Received</h2>
                <p>Hi {Encode(customerName)},</p>
                <p>We have received your payment for booking <strong>#{bookingId}</strong>.</p>
                <table style="border-collapse:collapse;">
                    <tr><td style="padding:4px 12px;font-weight:bold;">Amount Paid</td><td style="padding:4px 12px;">{amount:C}</td></tr>
                    <tr><td style="padding:4px 12px;font-weight:bold;">Remaining Balance</td><td style="padding:4px 12px;">{balanceDue:C}</td></tr>
                </table>
                <p>Thank you!</p>
                """;

            await SendAsync(toEmail, subject, body);
        }

        public async Task SendRequestStatusAsync(string toEmail, string customerName,
            string requestType, string status, string? adminNote = null)
        {
            var subject = $"Request Update - {requestType} {status}";
            var body = $"""
                <h2>Request {Encode(status)}</h2>
                <p>Hi {Encode(customerName)},</p>
                <p>Your <strong>{Encode(requestType)}</strong> request has been <strong>{Encode(status.ToLower())}</strong>.</p>
                """;

            if (!string.IsNullOrWhiteSpace(adminNote))
                body += $"<p><em>Note: {Encode(adminNote)}</em></p>";

            body += "<p>Thank you for using WebHotel.</p>";

            await SendAsync(toEmail, subject, body);
        }

        private async Task SendAsync(string to, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl
                };

                if (!string.IsNullOrWhiteSpace(_settings.Username))
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

                var from = new MailAddress(_settings.FromAddress, _settings.FromName);
                using var message = new MailMessage(from, new MailAddress(to))
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {To}: {Subject}. Email delivery is non-blocking.", to, subject);
            }
        }

        private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
    }
}
