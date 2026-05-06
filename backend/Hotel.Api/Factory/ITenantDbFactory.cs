using Hotel.Api.Data;

public interface ITenantDbFactory
{
    Task<AppDbContext> CreateAsync(string branchCode, CancellationToken ct = default);
}