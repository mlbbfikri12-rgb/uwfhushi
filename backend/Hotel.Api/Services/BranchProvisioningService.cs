using System.Text.RegularExpressions;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hotel.Api.Services;

public interface IBranchProvisioningService
{
    Task<IReadOnlyCollection<BranchResponseDto>> GetBranchesAsync();
    Task<BranchResponseDto?> GetBranchByIdAsync(Guid id);
    Task<BranchResponseDto> CreateBranchAsync(CreateBranchDto dto);
    Task<BranchResponseDto?> UpdateBranchStatusAsync(Guid id, bool isActive);
}

public class BranchProvisioningService : IBranchProvisioningService
{
    private static readonly Regex BranchCodePattern = new("^[A-Z0-9]{2,20}$", RegexOptions.Compiled);

    private readonly MasterDbContext _masterDb;
    private readonly IConfiguration _configuration;

    public BranchProvisioningService(MasterDbContext masterDb, IConfiguration configuration)
    {
        _masterDb = masterDb;
        _configuration = configuration;
    }

    public async Task<BranchResponseDto> CreateBranchAsync(CreateBranchDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Branch name is required");

        if (!BranchCodePattern.IsMatch(code))
            throw new Exception("Branch code must be 2-20 characters and contain only A-Z or 0-9");

        var exists = await _masterDb.Branches.AnyAsync(b => b.Code == code);
        if (exists)
            throw new Exception("Branch code already exists");

        var masterConnection = _configuration.GetConnectionString("Master");
        if (string.IsNullOrWhiteSpace(masterConnection))
            throw new InvalidOperationException("Master connection string is not configured");

        var masterBuilder = new NpgsqlConnectionStringBuilder(masterConnection);
        var dbName = $"hotel_{code.ToLowerInvariant()}";
        var dbHost = dto.DbHost?.Trim() ?? masterBuilder.Host ?? "localhost";
        var dbPort = dto.DbPort ?? masterBuilder.Port;
        var dbUser = dto.DbUser?.Trim() ?? masterBuilder.Username ?? "postgres";
        var dbPassword = dto.DbPassword ?? masterBuilder.Password ?? "postgres";

        var tenantBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = dbHost,
            Port = dbPort,
            Database = dbName,
            Username = dbUser,
            Password = dbPassword
        };

        await CreateDatabaseAsync(masterBuilder, dbName);
        await MigrateTenantDatabaseAsync(tenantBuilder.ConnectionString);

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            DbName = dbName,
            DbHost = dbHost,
            DbPort = dbPort,
            DbUser = dbUser,
            DbPassword = dbPassword,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _masterDb.Branches.Add(branch);
        await _masterDb.SaveChangesAsync();

        return new BranchResponseDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Code = branch.Code,
            DbName = branch.DbName,
            IsActive = branch.IsActive
        };
    }

    public async Task<IReadOnlyCollection<BranchResponseDto>> GetBranchesAsync()
    {
        return await _masterDb.Branches
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .Select(b => new BranchResponseDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                DbName = b.DbName,
                IsActive = b.IsActive
            })
            .ToListAsync();
    }

    public async Task<BranchResponseDto?> GetBranchByIdAsync(Guid id)
    {
        return await _masterDb.Branches
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BranchResponseDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                DbName = b.DbName,
                IsActive = b.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<BranchResponseDto?> UpdateBranchStatusAsync(Guid id, bool isActive)
    {
        var branch = await _masterDb.Branches.FirstOrDefaultAsync(b => b.Id == id);
        if (branch == null)
            return null;

        branch.IsActive = isActive;
        await _masterDb.SaveChangesAsync();

        return new BranchResponseDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Code = branch.Code,
            DbName = branch.DbName,
            IsActive = branch.IsActive
        };
    }

    private static async Task CreateDatabaseAsync(NpgsqlConnectionStringBuilder masterBuilder, string dbName)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(masterBuilder.ConnectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync();

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
        existsCommand.Parameters.AddWithValue("dbName", dbName);

        var exists = await existsCommand.ExecuteScalarAsync();
        if (exists != null)
            throw new Exception("Tenant database already exists");

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(dbName)}";
        await createCommand.ExecuteNonQueryAsync();
    }

    private static async Task MigrateTenantDatabaseAsync(string tenantConnectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(tenantConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}
