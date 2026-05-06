using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
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
    Task RegisterAsync(ClientRegisterDto dto);
    Task<ClientAuthResponseDto> LoginAsync(ClientLoginDto dto);
    Task<CustomerMeDto> GetMeAsync(Guid customerId);
    Task VerifyEmailAsync(string token);
}

public class ClientAuthService : IClientAuthService
{
    public const string CustomerRole = "CUSTOMER";

    private readonly MasterDbContext _masterDb;
    private readonly IConfiguration _configuration;
    private readonly IEmailQueue _emailQueue;

    public ClientAuthService(
        MasterDbContext masterDb,
        IConfiguration configuration,
        IEmailQueue emailQueue)
    {
        _masterDb = masterDb;
        _configuration = configuration;
        _emailQueue = emailQueue;
    }

    // =========================
    // REGISTER
    // =========================
    public async Task RegisterAsync(ClientRegisterDto dto)
    {
        ValidateRegister(dto);

        var email = dto.Email.Trim().ToLowerInvariant();

        var exists = await _masterDb.CustomersGlobal
            .AnyAsync(c => c.Email == email);

        if (exists)
            throw new Exception("Email already registered");

        var customer = new CustomerGlobal
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var token = Guid.NewGuid().ToString();

        _masterDb.CustomersGlobal.Add(customer);

        _masterDb.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Token = token,
            ExpiredAt = DateTime.UtcNow.AddHours(24)
        });

        await _masterDb.SaveChangesAsync();

        var verifyUrl = $"{_configuration["App:FrontendUrl"]}/verify?token={token}";

        // 🔥 QUEUE EMAIL (NON BLOCKING)
        _emailQueue.Enqueue(async ct =>
        {
            var subject = "Verify your account - Lynn Hotel";

            var html = $"""
<h2>Welcome, {customer.Name}</h2>

<p>Please verify your email:</p>

<p>
  <a href="{verifyUrl}" 
     style="display:inline-block;padding:10px 16px;background:#1a1f3c;color:white;text-decoration:none;border-radius:6px;">
     Verify Account
  </a>
</p>

<p>Or copy this link:</p>
<p>{verifyUrl}</p>

<p>This link expires in 24 hours.</p>
""";

            using var client = new SmtpClient(
                _configuration["Email:Host"],
                int.Parse(_configuration["Email:Port"]!))
            {
                EnableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true"),
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"])
            };

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _configuration["Email:FromEmail"],
                    _configuration["Email:FromName"]),
                Subject = subject,
                Body = html,
                IsBodyHtml = true
            };

            message.To.Add(customer.Email);

            await client.SendMailAsync(message, ct);
        });
    }

    // =========================
    // LOGIN
    // =========================
    public async Task<ClientAuthResponseDto> LoginAsync(ClientLoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var customer = await _masterDb.CustomersGlobal
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email);

        if (customer == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        // 🔥 guest account (booking tanpa login)
        if (string.IsNullOrWhiteSpace(customer.PasswordHash))
            throw new UnauthorizedAccessException("Account not activated");

        if (!customer.IsVerified)
            throw new UnauthorizedAccessException("Email not verified");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, customer.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        return BuildResponse(customer);
    }

    // =========================
    // VERIFY EMAIL
    // =========================
    public async Task VerifyEmailAsync(string token)
    {
        var data = await _masterDb.EmailVerificationTokens
            .FirstOrDefaultAsync(x => x.Token == token);

        if (data == null)
            throw new Exception("Invalid token");

        if (data.ExpiredAt < DateTime.UtcNow)
            throw new Exception("Token expired");

        var customer = await _masterDb.CustomersGlobal
            .FirstOrDefaultAsync(x => x.Id == data.CustomerId);

        if (customer == null)
            throw new Exception("Customer not found");

        if (!customer.IsVerified)
            customer.IsVerified = true;

        _masterDb.EmailVerificationTokens.Remove(data);

        await _masterDb.SaveChangesAsync();
    }

    // =========================
    // ME
    // =========================
    public async Task<CustomerMeDto> GetMeAsync(Guid customerId)
    {
        var customer = await _masterDb.CustomersGlobal
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null)
            throw new UnauthorizedAccessException("Customer not found");

        return new CustomerMeDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone
        };
    }

    // =========================
    // JWT
    // =========================
    private ClientAuthResponseDto BuildResponse(CustomerGlobal customer)
    {
        return new ClientAuthResponseDto
        {
            Token = GenerateToken(customer),
            Customer = new CustomerMeDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone
            }
        };
    }

    private string GenerateToken(CustomerGlobal customer)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new Exception("JWT Key not configured");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new("customer_id", customer.Id.ToString()),
            new("auth_type", "customer"),
            new(ClaimTypes.Role, CustomerRole)
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

    // =========================
    // VALIDATION
    // =========================
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