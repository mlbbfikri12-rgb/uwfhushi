using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Hotel.Api.Services;

public interface IStaffAuthService
{
    Task<StaffAuthResponseDto> LoginAsync(string email, string password);
    Task<StaffAuthResponseDto> GetMeAsync(Guid staffId);
}

public class StaffAuthService : IStaffAuthService
{
    private readonly MasterDbContext _masterDb;
    private readonly IConfiguration _configuration;

    public StaffAuthService(MasterDbContext masterDb, IConfiguration configuration)
    {
        _masterDb = masterDb;
        _configuration = configuration;
    }

    public async Task<StaffAuthResponseDto> LoginAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var staff = await _masterDb.Staffs
            .AsNoTracking()
            .Where(s => s.Email == normalizedEmail && s.IsActive)
            .Select(s => new StaffLoginRecord(
                s.Id,
                s.Email,
                s.PasswordHash,
                s.Role,
                s.StaffBranches.Select(sb => sb.BranchId).ToList()))
            .FirstOrDefaultAsync();

        if (staff == null || !BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash))
            throw new UnauthorizedAccessException("Invalid staff credentials");

        var allowedBranches = await GetAllowedBranchesAsync(staff.Role, staff.AllowedBranchIds);
        var allowedBranchIds = allowedBranches.Select(b => b.Id).ToList();

        var token = GenerateToken(staff.Id, staff.Role, allowedBranchIds);

        return new StaffAuthResponseDto
        {
            Token = token,
            StaffId = staff.Id,
            Role = staff.Role,
            AllowedBranchIds = allowedBranchIds,
            AllowedBranches = allowedBranches
        };
    }

    public async Task<StaffAuthResponseDto> GetMeAsync(Guid staffId)
    {
        var staff = await _masterDb.Staffs
            .AsNoTracking()
            .Where(s => s.Id == staffId && s.IsActive)
            .Select(s => new StaffLoginRecord(
                s.Id,
                s.Email,
                s.PasswordHash,
                s.Role,
                s.StaffBranches.Select(sb => sb.BranchId).ToList()))
            .FirstOrDefaultAsync();

        if (staff == null)
            throw new UnauthorizedAccessException("Staff not found");

        var allowedBranches = await GetAllowedBranchesAsync(staff.Role, staff.AllowedBranchIds);
        var allowedBranchIds = allowedBranches.Select(b => b.Id).ToList();

        return new StaffAuthResponseDto
        {
            Token = string.Empty,
            StaffId = staff.Id,
            Role = staff.Role,
            AllowedBranchIds = allowedBranchIds,
            AllowedBranches = allowedBranches
        };
    }

    private async Task<IReadOnlyCollection<StaffAllowedBranchDto>> GetAllowedBranchesAsync(string role, IReadOnlyCollection<Guid> branchIds)
    {
        var query = _masterDb.Branches.AsNoTracking().Where(b => b.IsActive);

        if (role != StaffRoles.SuperAdmin)
            query = query.Where(b => branchIds.Contains(b.Id));

        return await query
            .OrderBy(b => b.Code)
            .Select(b => new StaffAllowedBranchDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name
            })
            .ToListAsync();
    }

    private string GenerateToken(Guid staffId, string role, IReadOnlyCollection<Guid> allowedBranchIds)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT key is not configured");

        var claims = new List<Claim>
        {
            new("staff_id", staffId.ToString()),
            new("auth_type", "staff"),
            new("role", role),
            new(ClaimTypes.Role, role),
            new("allowed_branch_ids", string.Join(',', allowedBranchIds))
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record StaffLoginRecord(
        Guid Id,
        string Email,
        string PasswordHash,
        string Role,
        IReadOnlyCollection<Guid> AllowedBranchIds);
}
