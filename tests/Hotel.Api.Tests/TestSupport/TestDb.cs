using Hotel.Api.Data;
using Hotel.Api.Entities.Master;
using Hotel.Api.Entities.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hotel.Api.Tests.TestSupport;

public static class TestDb
{
    public static AppDbContext CreateTenantDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tenant-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    public static MasterDbContext CreateMasterDb()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseInMemoryDatabase($"master-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new MasterDbContext(options);
    }

    public static async Task<(RoomType RoomType, Room Room)> SeedRoomAsync(
        AppDbContext db,
        string roomNumber = "101",
        string status = "available",
        decimal basePrice = 500000m,
        int maxAdults = 2,
        int maxChildren = 1)
    {
        var roomType = new RoomType
        {
            Id = Guid.NewGuid(),
            Name = "Deluxe",
            Description = "Deluxe room",
            BasePrice = basePrice,
            MaxAdults = maxAdults,
            MaxChildren = maxChildren,
            CreatedAt = DateTime.UtcNow
        };

        var room = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = roomNumber,
            RoomTypeId = roomType.Id,
            RoomType = roomType,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        db.RoomTypes.Add(roomType);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        return (roomType, room);
    }

    public static async Task<Branch> SeedBranchAsync(
        MasterDbContext db,
        string code = "SBY",
        bool isActive = true)
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = $"Hotel {code}",
            Code = code,
            DbName = $"hotel_{code.ToLowerInvariant()}",
            DbHost = "localhost",
            DbPort = 5432,
            DbUser = "postgres",
            DbPassword = "postgres",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        return branch;
    }

    public static async Task<Staff> SeedStaffAsync(
        MasterDbContext db,
        string email = "fo@test.local",
        string password = "Password123!",
        string role = StaffRoles.FO,
        bool isActive = true,
        params Guid[] branchIds)
    {
        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            Name = "Staff Test",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Staffs.Add(staff);

        foreach (var branchId in branchIds)
        {
            db.StaffBranches.Add(new StaffBranch
            {
                Id = Guid.NewGuid(),
                StaffId = staff.Id,
                Staff = staff,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        return staff;
    }
}
