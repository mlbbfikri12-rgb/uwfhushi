using Hotel.Api.Entities.Tenant;

namespace Hotel.Api.Services;

public class NoopBookingEmailService : IBookingEmailService
{
    public Task SendBookingCreatedAsync(Booking booking, string hotelName, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
