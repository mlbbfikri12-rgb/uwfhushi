using Hotel.Api.Data;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public class HotelPriceSummaryService
{
    private readonly MasterDbContext _masterDb;
    private readonly ITenantDbFactory _tenantFactory;
    private readonly ILogger<HotelPriceSummaryService> _logger;

    public HotelPriceSummaryService(
        MasterDbContext masterDb,
        ITenantDbFactory tenantFactory,
        ILogger<HotelPriceSummaryService> logger)
    {
        _masterDb = masterDb;
        _tenantFactory = tenantFactory;
        _logger = logger;
    }

    public async Task UpdateAllAsync(CancellationToken ct)
    {
        var hotels = await _masterDb.Hotels
            .AsNoTracking()
            .Where(h => h.IsActive)
            .Select(h => new { h.Slug, h.BranchCode })
            .ToListAsync(ct);

        foreach (var hotel in hotels)
        {
            try
            {
                var lowestPrice = await GetLowestPriceFromTenant(hotel.BranchCode, ct);

                await UpsertAsync(hotel.Slug, lowestPrice, ct);

                _logger.LogInformation("Updated price summary {Slug} = {Price}", hotel.Slug, lowestPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed updating price summary for {Slug}", hotel.Slug);
            }
        }
    }

    public async Task UpdateByBranchCodeAsync(string branchCode, CancellationToken ct)
    {
        var hotel = await _masterDb.Hotels
            .AsNoTracking()
            .Where(h => h.BranchCode == branchCode && h.IsActive)
            .Select(h => new { h.Slug, h.BranchCode })
            .FirstOrDefaultAsync(ct);

        if (hotel == null)
        {
            _logger.LogWarning("Skip price summary update. Active hotel not found for branch {BranchCode}", branchCode);
            return;
        }

        var lowestPrice = await GetLowestPriceFromTenant(hotel.BranchCode, ct);
        await UpsertAsync(hotel.Slug, lowestPrice, ct);

        _logger.LogInformation(
            "Updated price summary for branch {BranchCode}. Slug={Slug}, LowestPrice={LowestPrice}",
            hotel.BranchCode,
            hotel.Slug,
            lowestPrice);
    }

    public async Task UpdateBySlugAsync(string slug, CancellationToken ct)
    {
        var hotel = await _masterDb.Hotels
            .AsNoTracking()
            .Where(h => h.Slug == slug && h.IsActive)
            .Select(h => new { h.Slug, h.BranchCode })
            .FirstOrDefaultAsync(ct);

        if (hotel == null)
        {
            _logger.LogWarning("Skip price summary update. Active hotel not found for slug {Slug}", slug);
            return;
        }

        var lowestPrice = await GetLowestPriceFromTenant(hotel.BranchCode, ct);
        await UpsertAsync(hotel.Slug, lowestPrice, ct);
    }

    private async Task<decimal> GetLowestPriceFromTenant(string branchCode, CancellationToken ct)
    {
        // 🔥 Ambil DB tenant
        await using var db = await _tenantFactory.CreateAsync(branchCode, ct);

        var price = await db.RatePlans
            .AsNoTracking()
            .Where(rp => rp.IsActive)
            .MinAsync(rp => (decimal?)rp.Price, ct);

        return price ?? 0;
    }

    private async Task UpsertAsync(string slug, decimal price, CancellationToken ct)
    {
        var existing = await _masterDb.HotelPriceSummaries
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        if (existing == null)
        {
            _masterDb.HotelPriceSummaries.Add(new HotelPriceSummary
            {
                Id = Guid.NewGuid(),
                Slug = slug,
                LowestPrice = price,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.LowestPrice = price;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _masterDb.SaveChangesAsync(ct);
    }
}
