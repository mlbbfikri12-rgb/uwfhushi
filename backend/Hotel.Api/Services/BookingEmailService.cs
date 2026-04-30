using System.Net;
using System.Net.Mail;
using System.Text;
using Hotel.Api.Configurations;
using Hotel.Api.Entities.Tenant;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IBookingEmailService
{
    Task SendBookingCreatedAsync(Booking booking, string hotelName, CancellationToken cancellationToken = default);
}

public class BookingEmailService : IBookingEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<BookingEmailService> _logger;

    public BookingEmailService(IOptions<EmailSettings> settings, ILogger<BookingEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendBookingCreatedAsync(Booking booking, string hotelName, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email sending skipped because Email:Enabled is false");
            return;
        }

        var customer = booking.Customer;
        var room = booking.Room;
        if (customer == null || room == null)
        {
            _logger.LogWarning("Email sending skipped because booking navigation data is incomplete. BookingId={BookingId}", booking.Id);
            return;
        }

        var html = BuildHtml(booking, hotelName);
        var subject = $"Booking {booking.BookingCode}";

        await SendWithRetryAsync(customer.Email, subject, html, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_settings.InternalEmail))
            await SendWithRetryAsync(_settings.InternalEmail, subject, html, cancellationToken);
    }

    private async Task SendWithRetryAsync(string to, string subject, string html, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, _settings.RetryCount);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl
                };

                if (!string.IsNullOrWhiteSpace(_settings.Username))
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = html,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                message.To.Add(to);

                _logger.LogInformation("Sending booking email to {Recipient}, attempt {Attempt}", to, attempt);
                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Booking email sent to {Recipient}", to);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                _logger.LogWarning(ex, "Booking email send failed to {Recipient}, retrying attempt {Attempt}", to, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking email send failed to {Recipient}", to);
            }
        }
    }

    private static string BuildHtml(Booking booking, string hotelName)
    {
        var customer = booking.Customer!;
        var room = booking.Room!;

        return $"""
        <html>
        <body style="font-family:Arial,sans-serif;color:#111827">
          <h2>Booking berhasil</h2>
          <p>Halo {WebUtility.HtmlEncode(customer.Name)}, booking Anda berhasil dibuat.</p>
          <table cellpadding="6" cellspacing="0" style="border-collapse:collapse">
            <tr><td><strong>Booking Code</strong></td><td>{WebUtility.HtmlEncode(booking.BookingCode)}</td></tr>
            <tr><td><strong>Hotel</strong></td><td>{WebUtility.HtmlEncode(hotelName)}</td></tr>
            <tr><td><strong>Tanggal</strong></td><td>{booking.CheckIn:yyyy-MM-dd} - {booking.CheckOut:yyyy-MM-dd}</td></tr>
            <tr><td><strong>Tamu</strong></td><td>{booking.AdultCount} adult, {booking.ChildCount} child</td></tr>
            <tr><td><strong>Kamar</strong></td><td>{WebUtility.HtmlEncode(room.RoomNumber)} - {WebUtility.HtmlEncode(room.RoomType.Name)}</td></tr>
            <tr><td><strong>Total</strong></td><td>{booking.TotalPrice:N2}</td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
