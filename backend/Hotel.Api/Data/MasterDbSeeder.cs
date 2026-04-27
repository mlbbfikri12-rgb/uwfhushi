using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Data;

public static class MasterDbSeeder
{
    public static async Task SeedAsync(MasterDbContext db)
    {
        var branches = new[]
        {
            new BranchSeed("Hotel Surabaya", "SBY", "hotel_sby"),
            new BranchSeed("Hotel Mojokerto", "MJK", "hotel_mjk"),
            new BranchSeed("Hotel Jakarta", "JKT", "hotel_jkt")
        };

        foreach (var seed in branches)
        {
            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == seed.Code);

            if (branch == null)
            {
                db.Branches.Add(new Branch
                {
                    Id = Guid.NewGuid(),
                    Name = seed.Name,
                    Code = seed.Code,
                    DbName = seed.DbName,
                    DbHost = "localhost",
                    DbPort = 5432,
                    DbUser = "postgres",
                    DbPassword = "postgres",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();

        var superAdmin = await db.Staffs.FirstOrDefaultAsync(s => s.Email == "superadmin@hotel.test");
        if (superAdmin == null)
        {
            db.Staffs.Add(new Staff
            {
                Id = Guid.NewGuid(),
                Name = "Super Admin",
                Email = "superadmin@hotel.test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = StaffRoles.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var sby = await db.Branches.FirstAsync(b => b.Code == "SBY");
        await EnsureBranchStaffAsync(db, "spv.sby@hotel.test", "SPV Surabaya", StaffRoles.SPV, sby.Id);
        await EnsureBranchStaffAsync(db, "fo.sby@hotel.test", "FO Surabaya", StaffRoles.FO, sby.Id);

        await db.SaveChangesAsync();
    }

    private static async Task EnsureBranchStaffAsync(
        MasterDbContext db,
        string email,
        string name,
        string role,
        Guid branchId)
    {
        var staff = await db.Staffs
            .Include(s => s.StaffBranches)
            .FirstOrDefaultAsync(s => s.Email == email);

        if (staff == null)
        {
            staff = new Staff
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Staffs.Add(staff);
        }

        if (staff.StaffBranches.All(sb => sb.BranchId != branchId))
        {
            db.StaffBranches.Add(new StaffBranch
            {
                Id = Guid.NewGuid(),
                StaffId = staff.Id,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private sealed record BranchSeed(string Name, string Code, string DbName);
}
