using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Tests.TestSupport;

public sealed class TestTenantDbFactory : ITenantDbFactory
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TestTenantDbFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public Task<AppDbContext> CreateAsync(string branchCode, CancellationToken ct)
    {
        return Task.FromResult(new AppDbContext(_options));
    }
}

public sealed class NoopHotelPriceSummaryUpdater : IHotelPriceSummaryUpdater
{
    public ValueTask EnqueueBranchAsync(string branchCode, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask EnqueueSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

public static class TestServices
{
    public static HttpContextAccessor CreateHttpContextAccessor(string branchCode = "SBY")
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Branch-Code"] = branchCode;

        return new HttpContextAccessor
        {
            HttpContext = context
        };
    }

    public static BookingService CreateBookingService(
        DbContextOptions<AppDbContext> options,
        MasterDbContext masterDb,
        string branchCode = "SBY")
    {
        return new BookingService(new TestTenantDbFactory(options), masterDb);
    }

    public static RoomManagementService CreateRoomManagementService(
        DbContextOptions<AppDbContext> options,
        string branchCode = "SBY")
    {
        return new RoomManagementService(
            new TestTenantDbFactory(options),
            new NoopCacheService(),
            CreateHttpContextAccessor(branchCode),
            Options.Create(new CacheSettings()),
            Options.Create(new BookingValidationSettings()),
            new NoopHotelPriceSummaryUpdater(),
            NullLogger<RoomManagementService>.Instance);
    }

    public static PaymentService CreatePaymentService(
        DbContextOptions<AppDbContext> options,
        MasterDbContext masterDb,
        string branchCode = "SBY")
    {
        return new PaymentService(
            new TestTenantDbFactory(options),
            new RoomAssignmentService(NullLogger<RoomAssignmentService>.Instance),
            masterDb,
            new NoopBookingEmailService(),
            new NoopEmailQueue(),
            new NoopCacheService(),
            NullLogger<PaymentService>.Instance);
    }
}
