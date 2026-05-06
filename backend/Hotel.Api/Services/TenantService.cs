using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterDbContext _masterDb;
    private readonly ICacheService _cache;

    public TenantService(
        IHttpContextAccessor httpContextAccessor,
        MasterDbContext masterDb,
        ICacheService cache)
    {
        _httpContextAccessor = httpContextAccessor;
        _masterDb = masterDb;
        _cache = cache;
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken ct = default)
    {
        var context = _httpContextAccessor.HttpContext;

        // 🔹 fallback untuk background process / non-http
        if (context == null)
        {
            return Environment.GetEnvironmentVariable("TENANT_CONNECTION_STRING")
                ?? "Host=localhost;Port=5432;Database=hotel_sby;Username=postgres;Password=postgres";
        }

        var branchCode = context.Request.Headers["X-Branch-Code"]
            .ToString()
            .Trim()
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(branchCode))
            throw TenantResolutionException.MissingBranchHeader();

        var cacheKey = $"tenant:{branchCode}";

        // 🔥 1. coba ambil dari cache
        var cached = await _cache.GetAsync<TenantCacheDto>(cacheKey, ct);
        if (cached == null)
        {
            // 🔥 2. fallback ke DB (ASYNC)
            var branch = await _masterDb.Branches
                .AsNoTracking()
                .Where(b => b.Code == branchCode && b.IsActive)
                .Select(b => new TenantCacheDto
                {
                    BranchId = b.Id,
                    ConnectionString =
                        $"Host={b.DbHost};Port={b.DbPort};Database={b.DbName};Username={b.DbUser};Password={b.DbPassword}"
                })
                .FirstOrDefaultAsync(ct);

            if (branch == null)
                throw TenantResolutionException.BranchNotFound();

            // 🔥 3. simpan ke cache
            await _cache.SetAsync(cacheKey, branch, TimeSpan.FromHours(1), ct);

            cached = branch;
        }

        // 🔥 4. VALIDASI AKSES STAFF
        var user = context.User;

        var isStaffContext =
            user?.HasClaim("auth_type", "staff") == true ||
            user?.IsInRole(StaffRoles.SuperAdmin) == true ||
            user?.IsInRole(StaffRoles.SPV) == true ||
            user?.IsInRole(StaffRoles.FO) == true;

        var isSuperAdmin = user?.IsInRole(StaffRoles.SuperAdmin) == true;

        if (isStaffContext && !isSuperAdmin)
        {
            var allowedBranchIds = (user?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                .Where(c => c.Type == "allowed_branch_ids")
                .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!allowedBranchIds.Contains(cached.BranchId.ToString()))
            {
                throw TenantResolutionException.ForbiddenBranchAccess();
            }
        }

        return cached.ConnectionString;
    }
}