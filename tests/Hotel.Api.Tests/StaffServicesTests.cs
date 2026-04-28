using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Hotel.Api.Services;
using Hotel.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hotel.Api.Tests;

public class StaffServicesTests
{
    [Fact]
    public async Task CreateStaff_WithValidRole_CreatesActiveStaff()
    {
        await using var db = TestDb.CreateMasterDb();
        var service = new StaffAdminService(db);

        var staff = await service.CreateStaffAsync(new CreateStaffDto
        {
            Name = "FO Test",
            Email = "FO@Test.Local",
            Password = "Password123!",
            Role = "fo"
        });

        Assert.Equal("fo@test.local", staff.Email);
        Assert.Equal(StaffRoles.FO, staff.Role);
        Assert.True(staff.IsActive);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", await db.Staffs.Select(s => s.PasswordHash).SingleAsync()));
    }

    [Fact]
    public async Task CreateStaff_WhenRoleInvalid_Throws()
    {
        await using var db = TestDb.CreateMasterDb();
        var service = new StaffAdminService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateStaffAsync(new CreateStaffDto
        {
            Name = "Invalid",
            Email = "invalid@test.local",
            Password = "Password123!",
            Role = "OWNER"
        }));

        Assert.Equal("Staff role must be SUPER_ADMIN, SPV, or FO", ex.Message);
    }

    [Fact]
    public async Task CreateStaff_WhenEmailDuplicate_Throws()
    {
        await using var db = TestDb.CreateMasterDb();
        await TestDb.SeedStaffAsync(db, email: "duplicate@test.local");
        var service = new StaffAdminService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateStaffAsync(new CreateStaffDto
        {
            Name = "Duplicate",
            Email = "duplicate@test.local",
            Password = "Password123!",
            Role = StaffRoles.FO
        }));

        Assert.Equal("Staff email already exists", ex.Message);
    }

    [Fact]
    public async Task AssignAndRemoveBranch_UpdatesStaffBranchAccess()
    {
        await using var db = TestDb.CreateMasterDb();
        var branch = await TestDb.SeedBranchAsync(db, "SMG");
        var staff = await TestDb.SeedStaffAsync(db);
        var service = new StaffAdminService(db);

        var assigned = await service.AssignBranchAsync(staff.Id, branch.Id);
        Assert.NotNull(assigned);
        Assert.Single(assigned.Branches);
        Assert.Equal("SMG", assigned.Branches.Single().Code);

        var removed = await service.RemoveBranchAsync(staff.Id, branch.Id);
        Assert.NotNull(removed);
        Assert.Empty(removed.Branches);
    }

    [Fact]
    public async Task Login_WithValidStaff_ReturnsTokenAndAllowedBranches()
    {
        await using var db = TestDb.CreateMasterDb();
        var branch = await TestDb.SeedBranchAsync(db, "SBY");
        var staff = await TestDb.SeedStaffAsync(
            db,
            email: "fo.sby@test.local",
            password: "Password123!",
            role: StaffRoles.FO,
            branchIds: branch.Id);

        var service = new StaffAuthService(db, CreateJwtConfiguration());

        var response = await service.LoginAsync("FO.SBY@Test.Local", "Password123!");

        Assert.Equal(staff.Id, response.StaffId);
        Assert.Equal(StaffRoles.FO, response.Role);
        Assert.Contains(branch.Id, response.AllowedBranchIds);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsUnauthorized()
    {
        await using var db = TestDb.CreateMasterDb();
        await TestDb.SeedStaffAsync(db, email: "fo@test.local", password: "Password123!");
        var service = new StaffAuthService(db, CreateJwtConfiguration());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync("fo@test.local", "wrong-password"));

        Assert.Equal("Invalid staff credentials", ex.Message);
    }

    [Fact]
    public async Task Login_SuperAdmin_ReturnsAllActiveBranches()
    {
        await using var db = TestDb.CreateMasterDb();
        var activeBranch = await TestDb.SeedBranchAsync(db, "SBY", isActive: true);
        var inactiveBranch = await TestDb.SeedBranchAsync(db, "OLD", isActive: false);
        await TestDb.SeedStaffAsync(db, email: "super@test.local", role: StaffRoles.SuperAdmin);
        var service = new StaffAuthService(db, CreateJwtConfiguration());

        var response = await service.LoginAsync("super@test.local", "Password123!");

        Assert.Contains(activeBranch.Id, response.AllowedBranchIds);
        Assert.DoesNotContain(inactiveBranch.Id, response.AllowedBranchIds);
    }

    private static IConfiguration CreateJwtConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-with-enough-length",
                ["Jwt:Issuer"] = "hotel-system-test",
                ["Jwt:Audience"] = "hotel-system-test"
            })
            .Build();
    }
}
