using Hotel.Api.Data;
using Microsoft.EntityFrameworkCore;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IStaffAuthService, StaffAuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBranchProvisioningService, BranchProvisioningService>();
builder.Services.AddScoped<IStaffAdminService, StaffAdminService>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();

builder.Services.AddDbContext<MasterDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Master"));
});

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var tenantService = sp.GetRequiredService<ITenantService>();
    options.UseNpgsql(tenantService.GetConnectionString());
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrWhiteSpace(jwtKey))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("seed-master", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
    await MasterDbSeeder.SeedAsync(db);
    Console.WriteLine("Master database seeded.");
    return;
}

if (args.Contains("seed-tenant", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
    Console.WriteLine("Tenant database seeded.");
    return;
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (TenantResolutionException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

if (!string.IsNullOrWhiteSpace(jwtKey))
{
    app.UseAuthentication();
}
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
