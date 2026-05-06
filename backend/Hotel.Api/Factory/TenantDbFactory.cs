using Hotel.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public class TenantDbFactory : ITenantDbFactory
{
    private readonly MasterDbContext _masterDb;

    public TenantDbFactory(MasterDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    public async Task<AppDbContext> CreateAsync(string branchCode, CancellationToken ct)
    {
        var branch = await _masterDb.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == branchCode && b.IsActive, ct);

        if (branch == null)
            throw new Exception("Branch not found");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql($"Host={branch.DbHost};Port={branch.DbPort};Database={branch.DbName};Username={branch.DbUser};Password={branch.DbPassword}")
            .Options;

        return new AppDbContext(options);
    }
}