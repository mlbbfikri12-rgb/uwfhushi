using StackExchange.Redis;

namespace Hotel.Api.Services;

public interface IDistributedLockService
{
    Task<string?> AcquireAsync(string key, TimeSpan expiry);
    Task ReleaseAsync(string key, string lockToken);
}

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string?> AcquireAsync(string key, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();

        var token = Guid.NewGuid().ToString();

        var acquired = await db.StringSetAsync(
            key,
            token,
            expiry,
            when: When.NotExists);

        return acquired ? token : null;
    }

    public async Task ReleaseAsync(string key, string lockToken)
    {
        var db = _redis.GetDatabase();

        const string script = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            else
                return 0
            end";

        await db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key },
            new RedisValue[] { lockToken });
    }
}