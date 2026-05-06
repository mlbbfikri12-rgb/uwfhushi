public interface ITenantService
{
    Task<string> GetConnectionStringAsync(CancellationToken ct = default);
}