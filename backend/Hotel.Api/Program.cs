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

// 🔥 ADD THIS
var isSeedPrice = args.Contains("seed-price", StringComparer.OrdinalIgnoreCase);

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

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Rate limit exceeded" },
            cancellationToken);
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

builder.Services.AddScoped<ITenantDbFactory, TenantDbFactory>();


// ======================
// 🔥 CONDITIONAL SERVICES (FIX DI CRASH)
// ======================
if (!isSeedPrice)
{
    builder.Services.AddScoped<ITenantService, TenantService>();

    builder.Services.AddScoped<IBookingService, BookingService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IBookingExpiryService, BookingExpiryService>();
    builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
    builder.Services.AddScoped<IRoomAssignmentService, RoomAssignmentService>();
    builder.Services.AddScoped<IRatePlanAdminService, RatePlanAdminService>();
    builder.Services.AddScoped<ITenantSeedService, TenantSeedService>();
}

// ======================
// SERVICES (TIDAK DIHAPUS)
// ======================
builder.Services.AddScoped<IStaffAuthService, StaffAuthService>();
builder.Services.AddScoped<IClientAuthService, ClientAuthService>();
builder.Services.AddScoped<IBranchProvisioningService, BranchProvisioningService>();
builder.Services.AddScoped<IStaffAdminService, StaffAdminService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IPublicBranchService, PublicBranchService>();
builder.Services.AddScoped<IPublicHotelSearchService, PublicHotelSearchService>();
builder.Services.AddScoped<IHotelPublicService, HotelPublicService>();
builder.Services.AddScoped<IPublicHomeService, PublicHomeService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IHotelAdminService, HotelAdminService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<EmailQueue>());
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<PendingBookingExpiryBackgroundService>();
builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddScoped<IBookingEmailService, BookingEmailService>();
builder.Services.AddHttpClient<IObjectStorageService, ObjectStorageService>();

// 🔥 ADD THIS (WAJIB)
builder.Services.AddScoped<HotelPriceSummaryService>();

builder.Services.AddDbContextPool<MasterDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Master"));
});

builder.Services.AddPooledDbContextFactory<MasterDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Master"));
});

builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql();
});

// AUTH

// AUTH
var jwtKey = builder.Configuration["Jwt:Key"];

if (!string.IsNullOrWhiteSpace(jwtKey) && !isSeedPrice)
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

                    // =========================
                    // 🔓 PUBLIC (NO AUTH)
                    // =========================
                    if (path.StartsWithSegments("/api/public", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/hotel", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.CompletedTask;
                    }

                    // =========================
                    // 🔐 STAFF TOKEN
                    // =========================
                    if (
                        path.StartsWithSegments("/api/branches", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/staff", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/rooms", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/room-types", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/auth/staff", StringComparison.OrdinalIgnoreCase) || // 🔥 FIX UTAMA
                        path.StartsWithSegments("/auth/staff", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/api/uploads", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        var staffToken = context.Request.Cookies["staff_token"];

                        if (!string.IsNullOrWhiteSpace(staffToken))
                        {
                            context.Token = staffToken;
                        }

                        return Task.CompletedTask;
                    }

                    // =========================
                    // 🔄 BOOKING (FLEXIBLE)
                    // =========================
                    if (path.StartsWithSegments("/api/booking", StringComparison.OrdinalIgnoreCase))
                    {
                        var customerToken = context.Request.Cookies["customer_token"];

                        if (!string.IsNullOrWhiteSpace(customerToken))
                        {
                            context.Token = customerToken;
                        }

                        return Task.CompletedTask;
                    }

                    // =========================
                    // 👤 CUSTOMER REQUIRED
                    // =========================
                    if (
                        path.StartsWithSegments("/api/auth/me", StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWithSegments("/auth/me", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        context.Token = context.Request.Cookies["customer_token"];
                        return Task.CompletedTask;
                    }

                    // =========================
                    // 🔄 DEFAULT (CUSTOMER)
                    // =========================
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

// ======================
// 🔥 SEED PRICE MODE
// ======================
if (isSeedPrice)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("SeedPrice");

    try
    {
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        await using var handle = await lockService.AcquireAsync(
            "seed:hotel-price-summary",
            TimeSpan.FromMinutes(5));

        var service = scope.ServiceProvider.GetRequiredService<HotelPriceSummaryService>();

        logger.LogInformation("Starting HotelPriceSummary seed...");

        await service.UpdateAllAsync(CancellationToken.None);

        logger.LogInformation("HotelPriceSummary seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "HotelPriceSummary seed failed.");
    }

    return;
}

// ======================
// PIPELINE NORMAL
// ======================
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (TenantResolutionException ex)
    {
        context.Response.StatusCode = ex.StatusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                message = ex.Message,
                code = ex.ErrorCode
            }
        });
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
