using System.Security.Claims;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Hotel.Api.Tests.TestSupport;
using Microsoft.AspNetCore.Http;

namespace Hotel.Api.Tests;

public class TenantServiceTests
{
    [Fact]
    public async Task GetConnectionString_WhenHeaderMissing_ThrowsBadRequest()
    {
        await using var db = TestDb.CreateMasterDb();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        var service = new TenantService(httpContextAccessor, db, new NoopCacheService());

        var ex = await Assert.ThrowsAsync<TenantResolutionException>(() => service.GetConnectionStringAsync());

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Equal("X-Branch-Code header is missing", ex.Message);
    }

    [Fact]
    public async Task GetConnectionString_WithValidBranchHeader_ReturnsTenantConnection()
    {
        await using var db = TestDb.CreateMasterDb();
        await TestDb.SeedBranchAsync(db, "SMG");
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Branch-Code"] = "smg";
        var service = new TenantService(new HttpContextAccessor { HttpContext = context }, db, new NoopCacheService());

        var connectionString = await service.GetConnectionStringAsync();

        Assert.Contains("Database=hotel_smg", connectionString);
    }

    [Fact]
    public async Task GetConnectionString_WhenAuthenticatedStaffCannotAccessBranch_ThrowsForbidden()
    {
        await using var db = TestDb.CreateMasterDb();
        await TestDb.SeedBranchAsync(db, "SMG");
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Branch-Code"] = "SMG";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, StaffRoles.FO),
            new Claim("allowed_branch_ids", Guid.NewGuid().ToString())
        }, "jwt"));

        var service = new TenantService(new HttpContextAccessor { HttpContext = context }, db, new NoopCacheService());

        var ex = await Assert.ThrowsAsync<TenantResolutionException>(() => service.GetConnectionStringAsync());

        Assert.Equal(StatusCodes.Status403Forbidden, ex.StatusCode);
        Assert.Equal("You are not allowed to access this branch", ex.Message);
    }
}
