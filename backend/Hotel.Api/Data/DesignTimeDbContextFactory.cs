using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hotel.Api.Data;

public class MasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();
        optionsBuilder.UseNpgsql(GetConnectionString(
            "MASTER_CONNECTION_STRING",
            "Host=localhost;Port=5432;Database=hotel_master;Username=postgres;Password=postgres"));

        return new MasterDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString(string variableName, string fallback)
    {
        return Environment.GetEnvironmentVariable(variableName) ?? fallback;
    }
}

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(GetConnectionString(
            "TENANT_CONNECTION_STRING",
            "Host=localhost;Port=5432;Database=hotel_sby;Username=postgres;Password=postgres"));

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString(string variableName, string fallback)
    {
        return Environment.GetEnvironmentVariable(variableName) ?? fallback;
    }
}
