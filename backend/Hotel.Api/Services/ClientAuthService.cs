using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hotel.Api.Data;
using Hotel.Api.DTOs;
using Hotel.Api.Entities.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Hotel.Api.Services;

public interface IClientAuthService
{
    Task<ClientAuthResponseDto> RegisterAsync(ClientRegisterDto dto);
    Task<ClientAuthResponseDto> LoginAsync(ClientLoginDto dto);
    Task<CustomerMeDto> GetMeAsync(Guid customerId);
}

public class ClientAuthService : IClientAuthService
{
    public const string CustomerRole = "CUSTOMER";

    private readonly MasterDbContext _masterDb;
    private readonly IConfiguration _configuration;

    public ClientAuthService(MasterDbContext masterDb, IConfiguration configuration)
    {
        _masterDb = masterDb;
        _configuration = configuration;
    }

    public async Task<ClientAuthResponseDto> RegisterAsync(ClientRegisterDto dto)
    {
        ValidateRegister(dto);

        var email = dto.Email.Trim().ToLowerInvariant();
        var exists = await _masterDb.CustomersGlobal.AnyAsync(c => c.Email == email);
        if (exists)
            throw new Exception("Email already registered");

        var customer = new CustomerGlobal
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _masterDb.CustomersGlobal.Add(customer);
        await _masterDb.SaveChangesAsync();

        return BuildResponse(customer);
    }

    public async Task<ClientAuthResponseDto> LoginAsync(ClientLoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new UnauthorizedAccessException("Invalid customer credentials");

        var customer = await _masterDb.CustomersGlobal
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email);

        if (customer == null ||
            string.IsNullOrWhiteSpace(customer.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid customer credentials");
        }

        return BuildResponse(customer);
    }

    public async Task<CustomerMeDto> GetMeAsync(Guid customerId)
    {
        var customer = await _masterDb.CustomersGlobal
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null)
            throw new UnauthorizedAccessException("Customer not found");

        return ToMeDto(customer);
    }

    private ClientAuthResponseDto BuildResponse(CustomerGlobal customer)
    {
        return new ClientAuthResponseDto
        {
            Token = GenerateToken(customer),
            Customer = ToMeDto(customer)
        };
    }

    private string GenerateToken(CustomerGlobal customer)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT key is not configured");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new("customer_id", customer.Id.ToString()),
            new("auth_type", "customer"),
            new("email", customer.Email),
            new(ClaimTypes.Role, CustomerRole),
            new("role", CustomerRole)
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

    private static CustomerMeDto ToMeDto(CustomerGlobal customer)
    {
        return new CustomerMeDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone
        };
    }

    private static void ValidateRegister(ClientRegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new Exception("Name is required");

        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
            throw new Exception("Valid email is required");

        if (string.IsNullOrWhiteSpace(dto.Phone))
            throw new Exception("Phone is required");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            throw new Exception("Password must be at least 8 characters");
    }
}
