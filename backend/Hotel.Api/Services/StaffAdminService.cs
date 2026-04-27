using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Api.Services;

public interface IStaffAdminService
{
    Task<IReadOnlyCollection<StaffResponseDto>> GetStaffsAsync();
    Task<StaffResponseDto?> GetStaffByIdAsync(Guid id);
    Task<StaffResponseDto> CreateStaffAsync(CreateStaffDto dto);
    Task<StaffResponseDto?> UpdateStaffStatusAsync(Guid id, bool isActive);
    Task<StaffResponseDto?> AssignBranchAsync(Guid staffId, Guid branchId);
    Task<StaffResponseDto?> RemoveBranchAsync(Guid staffId, Guid branchId);
}

public class StaffAdminService : IStaffAdminService
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        StaffRoles.SuperAdmin,
        StaffRoles.SPV,
        StaffRoles.FO
    };

    private readonly MasterDbContext _masterDb;

    public StaffAdminService(MasterDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    public async Task<IReadOnlyCollection<StaffResponseDto>> GetStaffsAsync()
    {
        return await BuildStaffQuery()
            .OrderBy(s => s.Email)
            .ToListAsync();
    }

    public async Task<StaffResponseDto?> GetStaffByIdAsync(Guid id)
    {
        return await BuildStaffQuery()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<StaffResponseDto> CreateStaffAsync(CreateStaffDto dto)
    {
        var name = dto.Name.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        var role = dto.Role.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
            throw new Exception("Staff name is required");

        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Staff email is required");

        if (dto.Password.Length < 8)
            throw new Exception("Staff password must be at least 8 characters");

        if (!AllowedRoles.Contains(role))
            throw new Exception("Staff role must be SUPER_ADMIN, SPV, or FO");

        var exists = await _masterDb.Staffs.AnyAsync(s => s.Email == email);
        if (exists)
            throw new Exception("Staff email already exists");

        var staff = new Staff
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _masterDb.Staffs.Add(staff);
        await _masterDb.SaveChangesAsync();

        return await GetStaffByIdAsync(staff.Id)
            ?? throw new Exception("Created staff could not be loaded");
    }

    public async Task<StaffResponseDto?> UpdateStaffStatusAsync(Guid id, bool isActive)
    {
        var staff = await _masterDb.Staffs.FirstOrDefaultAsync(s => s.Id == id);
        if (staff == null)
            return null;

        staff.IsActive = isActive;
        await _masterDb.SaveChangesAsync();

        return await GetStaffByIdAsync(id);
    }

    public async Task<StaffResponseDto?> AssignBranchAsync(Guid staffId, Guid branchId)
    {
        var staff = await _masterDb.Staffs.FirstOrDefaultAsync(s => s.Id == staffId);
        if (staff == null)
            return null;

        var branchExists = await _masterDb.Branches.AnyAsync(b => b.Id == branchId);
        if (!branchExists)
            throw new Exception("Branch not found");

        var assigned = await _masterDb.StaffBranches
            .AnyAsync(sb => sb.StaffId == staffId && sb.BranchId == branchId);

        if (!assigned)
        {
            _masterDb.StaffBranches.Add(new StaffBranch
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow
            });

            await _masterDb.SaveChangesAsync();
        }

        return await GetStaffByIdAsync(staffId);
    }

    public async Task<StaffResponseDto?> RemoveBranchAsync(Guid staffId, Guid branchId)
    {
        var staff = await _masterDb.Staffs.FirstOrDefaultAsync(s => s.Id == staffId);
        if (staff == null)
            return null;

        var assignment = await _masterDb.StaffBranches
            .FirstOrDefaultAsync(sb => sb.StaffId == staffId && sb.BranchId == branchId);

        if (assignment != null)
        {
            _masterDb.StaffBranches.Remove(assignment);
            await _masterDb.SaveChangesAsync();
        }

        return await GetStaffByIdAsync(staffId);
    }

    private IQueryable<StaffResponseDto> BuildStaffQuery()
    {
        return _masterDb.Staffs
            .AsNoTracking()
            .Select(s => new StaffResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                Role = s.Role,
                IsActive = s.IsActive,
                Branches = s.StaffBranches
                    .OrderBy(sb => sb.Branch.Code)
                    .Select(sb => new BranchResponseDto
                    {
                        Id = sb.Branch.Id,
                        Name = sb.Branch.Name,
                        Code = sb.Branch.Code,
                        DbName = sb.Branch.DbName,
                        IsActive = sb.Branch.IsActive
                    })
                    .ToList()
            });
    }
}
