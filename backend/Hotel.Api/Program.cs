using Hotel.Api.Data;
using Hotel.Api.Configurations;
using Hotel.Api.Middlewares;
using Microsoft.EntityFrameworkCore;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "FrontendCors";

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("auth_type", "customer");
    });

    options.AddPolicy("StaffOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("auth_type", "staff");
    });
});
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("Cache"));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<BookingValidationSettings>(builder.Configuration.GetSection("BookingValidation"));
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var origins = builder.Configuration["Cors:AllowedOrigins"];
        if (string.IsNullOrWhiteSpace(origins))
        {
            policy.AllowAnyHeader().AllowAnyMethod();
            return;
        }

        var allowedOrigins = origins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Cache:RedisConnection"];
});
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Cache:RedisConnection"] ?? "localhost:6379"));

var rateLimitSettings = builder.Configuration.GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiter");
        logger.LogWarning(
            "Rate limit reject. Path={Path}, Ip={Ip}",
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress);

        await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" }, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitSettings.GlobalPermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitSettings.GlobalWindowSeconds),
            QueueLimit = 0
        });
    });

    options.AddFixedWindowLimiter("booking", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings.BookingPermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings.BookingWindowSeconds);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-login", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings.AuthLoginPermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings.AuthWindowSeconds);
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-register", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitSettings.AuthRegisterPermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(rateLimitSettings.AuthWindowSeconds);
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IStaffAuthService, StaffAuthService>();
builder.Services.AddScoped<IClientAuthService, ClientAuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBranchProvisioningService, BranchProvisioningService>();
builder.Services.AddScoped<IStaffAdminService, StaffAdminService>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IPublicBranchService, PublicBranchService>();
builder.Services.AddScoped<IPublicHotelSearchService, PublicHotelSearchService>();
builder.Services.AddScoped<IHotelPublicService, HotelPublicService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IHotelAdminService, HotelAdminService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IBookingEmailService, BookingEmailService>();
builder.Services.AddScoped<ITenantSeedService, TenantSeedService>();
builder.Services.AddHttpClient<IObjectStorageService, ObjectStorageService>();

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
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var path = context.Request.Path;

                    // 🔓 1. PUBLIC → tidak pakai token sama sekali
                    if (path.StartsWithSegments("/api/public", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.CompletedTask;
                    }

                    // 🔐 2. STAFF ENDPOINT → pakai staff_token
                    if (
                        path.StartsWithSegments("/api/branches", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/staff", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/rooms", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/room-types", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/auth/staff", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/auth/staff", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/uploads", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        context.Token = context.Request.Cookies["staff_token"];
                        return Task.CompletedTask;
                    }

                    // 🔄 3. BOOKING (FLEXIBLE)
                    // 👉 kalau ada customer_token → pakai
                    // 👉 kalau tidak ada → biarkan anonymous (guest)
                    if (path.StartsWithSegments("/api/booking", StringComparison.OrdinalIgnoreCase))
                    {
                        var customerToken = context.Request.Cookies["customer_token"];

                        if (!string.IsNullOrWhiteSpace(customerToken))
                        {
                            context.Token = customerToken;
                        }

                        return Task.CompletedTask;
                    }

                    // 👤 4. CUSTOMER ENDPOINT (WAJIB AUTH)
                    if (
                        path.StartsWithSegments("/api/auth/me", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/auth/me", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        context.Token = context.Request.Cookies["customer_token"];
                        return Task.CompletedTask;
                    }

                    // 🔄 5. DEFAULT → pakai customer_token jika ada
                    var defaultToken = context.Request.Cookies["customer_token"];

                    if (!string.IsNullOrWhiteSpace(defaultToken))
                    {
                        context.Token = defaultToken;
                    }

                    return Task.CompletedTask;
                }
            };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };
        });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    var masterDb = startupScope.ServiceProvider.GetRequiredService<MasterDbContext>();
    await MasterDbSeeder.SeedAsync(masterDb);
    await MasterDataSeeder.SeedAsync(masterDb);
}

if (args.Contains("seed-master", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
    await MasterDbSeeder.SeedAsync(db);
    await MasterDataSeeder.SeedAsync(db);
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
app.UseCors(CorsPolicyName);
app.UseStaticFiles();
app.UseMiddleware<RedisBookingRateLimitMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
