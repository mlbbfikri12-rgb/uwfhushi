using Hotel.Api.Data;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterDbContext _masterDb;

    public TenantService(IHttpContextAccessor httpContextAccessor, MasterDbContext masterDb)
    {
        _httpContextAccessor = httpContextAccessor;
        _masterDb = masterDb;
    }

    public string GetConnectionString()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context == null)
        {
            return Environment.GetEnvironmentVariable("TENANT_CONNECTION_STRING")
                ?? "Host=localhost;Port=5432;Database=hotel_sby;Username=postgres;Password=postgres";
        }

        var branchCode = context.Request.Headers["X-Branch-Code"].ToString().Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new TenantResolutionException("X-Branch-Code header is missing", StatusCodes.Status400BadRequest);

        var branch = _masterDb.Branches
            .AsNoTracking()
            .FirstOrDefault(b => b.Code == branchCode && b.IsActive);

        if (branch == null)
            throw new TenantResolutionException("Branch not found", StatusCodes.Status400BadRequest);

        var isStaffContext =
            context.User?.HasClaim("auth_type", "staff") == true ||
            context.User?.IsInRole(StaffRoles.SuperAdmin) == true ||
            context.User?.IsInRole(StaffRoles.SPV) == true ||
            context.User?.IsInRole(StaffRoles.FO) == true;

        var isSuperAdmin = context.User?.IsInRole(StaffRoles.SuperAdmin) == true;

        if (isStaffContext && !isSuperAdmin)
        {
            var allowedBranchIds = (context.User?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                .Where(c => c.Type == "allowed_branch_ids")
                .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!allowedBranchIds.Contains(branch.Id.ToString()))
            {
                throw new TenantResolutionException(
                    "Staff is not allowed to access this branch",
                    StatusCodes.Status403Forbidden);
            }
        }

        return $"Host={branch.DbHost};Port={branch.DbPort};Database={branch.DbName};Username={branch.DbUser};Password={branch.DbPassword}";
    }
}
