using Hotel.Api.Configurations;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Hotel.Api.Middlewares;

public class RedisBookingRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<RedisBookingRateLimitMiddleware> _logger;

    public RedisBookingRateLimitMiddleware(
        RequestDelegate next,
        IOptions<RateLimitSettings> settings,
        ILogger<RedisBookingRateLimitMiddleware> logger)
    {
        _next = next;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConnectionMultiplexer redis)
    {
        var rule = GetRule(context);
        if (!_settings.UseRedisForBookingLimit || rule == null)
        {
            await _next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var branch = context.Request.Headers["X-Branch-Code"].ToString().Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(branch))
            branch = "unknown";

        var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / rule.WindowSeconds;
        var key = $"ratelimit:{rule.Name}:{branch}:{remoteIp}:{window}";
        var db = redis.GetDatabase();
        var count = await db.StringIncrementAsync(key);

        if (count == 1)
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(rule.WindowSeconds));

        if (count > rule.PermitLimit)
        {
            _logger.LogWarning("Rate limit reject for {Rule}. Branch={Branch}, Ip={Ip}", rule.Name, branch, remoteIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }

    private RateLimitRule? GetRule(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
            return null;

        var path = context.Request.Path;

        if (path.StartsWithSegments("/api/booking", StringComparison.OrdinalIgnoreCase))
            return new RateLimitRule("booking", _settings.BookingPermitLimit, _settings.BookingWindowSeconds);

        if (path.StartsWithSegments("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/register", StringComparison.OrdinalIgnoreCase))
            return new RateLimitRule("auth-register", _settings.AuthRegisterPermitLimit, _settings.AuthWindowSeconds);

        if (path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/auth/staff/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/staff/login", StringComparison.OrdinalIgnoreCase))
            return new RateLimitRule("auth-login", _settings.AuthLoginPermitLimit, _settings.AuthWindowSeconds);

        return null;
    }

    private sealed record RateLimitRule(string Name, int PermitLimit, int WindowSeconds);
}
