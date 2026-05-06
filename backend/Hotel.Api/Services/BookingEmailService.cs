using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Channels;
using Hotel.Api.Configurations;
using Hotel.Api.Entities.Tenant;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IBookingEmailService
{
    Task SendBookingCreatedAsync(Booking booking, string hotelName, CancellationToken cancellationToken = default);
}

// =========================
// 🔥 MAIN SERVICE
// =========================
public class BookingEmailService : IBookingEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<BookingEmailService> _logger;
    private readonly IEmailQueue _queue;

    public BookingEmailService(
        IOptions<EmailSettings> settings,
        ILogger<BookingEmailService> logger,
        IEmailQueue queue)
    {
        _settings = settings.Value;
        _logger = logger;
        _queue = queue;
    }

    public Task SendBookingCreatedAsync(Booking booking, string hotelName, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email skipped (disabled)");
            return Task.CompletedTask;
        }

        var customer = booking.Customer;
        var room = booking.Room;

        if (customer == null || room == null)
        {
            _logger.LogWarning("Email skipped (missing navigation). BookingId={BookingId}", booking.Id);
            return Task.CompletedTask;
        }

        var html = BuildHtml(booking, hotelName);
        var subject = $"Booking {booking.BookingCode}";

        // 🔥 enqueue (non-blocking)
        _queue.Enqueue(async ct =>
        {
            await SendWithRetryAsync(customer.Email, subject, html, ct);

            if (!string.IsNullOrWhiteSpace(_settings.InternalEmail))
                await SendWithRetryAsync(_settings.InternalEmail, subject, html, ct);
        });

        return Task.CompletedTask;
    }

    // =========================
    // 🔥 SMTP SEND + RETRY
    // =========================
    private async Task SendWithRetryAsync(string to, string subject, string html, CancellationToken cancellationToken)
    {
        var attempts = Math.Min(3, Math.Max(1, _settings.RetryCount));

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Timeout = 5000 // 🔥 prevent hang
                };

                if (!string.IsNullOrWhiteSpace(_settings.Username))
                {
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                }

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

                _logger.LogInformation("Sending email to {Recipient} (attempt {Attempt})", to, attempt);

                await client.SendMailAsync(message, cancellationToken);

                _logger.LogInformation("Email sent to {Recipient}", to);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                _logger.LogWarning(ex, "Retry email to {Recipient} (next attempt {Attempt})", to, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending email to {Recipient}", to);
            }
        }
    }

    // =========================
    // 🔥 HTML TEMPLATE
    // =========================
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
            <tr><td><strong>Total</strong></td><td>{booking.TotalPrice.ToString("N2", CultureInfo.InvariantCulture)}</td></tr>
          </table>
        </body>
        </html>
        """;
    }
}