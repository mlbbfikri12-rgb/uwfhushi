using Hotel.Api.Configurations;
using Hotel.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hotel.Api.Services;

public interface IBookingExpiryService
{
    Task<int> ExpirePendingBookingsAsync(string branchCode, CancellationToken ct = default);
}

public class BookingExpiryService : IBookingExpiryService
{
    private readonly ITenantDbFactory _tenantDbFactory;
    private readonly ICacheService _cache;
    private readonly BookingValidationSettings _settings;

    public BookingExpiryService(
        ITenantDbFactory tenantDbFactory,
        ICacheService cache,
        IOptions<BookingValidationSettings> settings)
    {
        _tenantDbFactory = tenantDbFactory;
        _cache = cache;
        _settings = settings.Value;
    }

    public async Task<int> ExpirePendingBookingsAsync(string branchCode, CancellationToken ct = default)
    {
        await using var db = await _tenantDbFactory.CreateAsync(branchCode, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var now = DateTime.UtcNow;
        var pendingToExpire = await db.Bookings
            .Where(b => b.Status == "pending" && b.HoldUntilUtc <= now)
            .ToListAsync(ct);

        if (pendingToExpire.Count == 0)
            return 0;

        foreach (var booking in pendingToExpire)
        {
            booking.Status = "expired";
            booking.PaymentStatus = "expired";
        }

        var affectedGroupIds = pendingToExpire.Select(b => b.BookingGroupId).Distinct().ToList();
        var groups = await db.BookingGroups
            .Where(g => affectedGroupIds.Contains(g.Id))
            .ToListAsync(ct);

        foreach (var group in groups)
        {
            var hasActive = await db.Bookings.AnyAsync(b =>
                b.BookingGroupId == group.Id &&
                (b.Status == "confirmed" || b.Status == "paid" || (b.Status == "pending" && b.HoldUntilUtc > now)), ct);

            if (!hasActive)
            {
                group.Status = "expired";
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await _cache.RemoveByPrefixAsync($"availability:{branchCode}:");
        await _cache.RemoveByPrefixAsync($"hotel:full:{branchCode}:");

        return pendingToExpire.Count;
    }
}

public class PendingBookingExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingBookingExpiryBackgroundService> _logger;
    private readonly BookingValidationSettings _settings;

    public PendingBookingExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingBookingExpiryBackgroundService> logger,
        IOptions<BookingValidationSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
                var expiryService = scope.ServiceProvider.GetRequiredService<IBookingExpiryService>();

                var branchCodes = await masterDb.Branches
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .Select(b => b.Code)
                    .ToListAsync(stoppingToken);

                foreach (var branchCode in branchCodes)
                {
                    var expiredCount = await expiryService.ExpirePendingBookingsAsync(branchCode, stoppingToken);
                    if (expiredCount > 0)
                    {
                        _logger.LogInformation("Expired {Count} pending bookings in branch {BranchCode}", expiredCount, branchCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending booking expiry background sweep failed");
            }

            var waitMinutes = Math.Max(1, _settings.PendingExpirySweepMinutes);
            await Task.Delay(TimeSpan.FromMinutes(waitMinutes), stoppingToken);
        }
    }
}
