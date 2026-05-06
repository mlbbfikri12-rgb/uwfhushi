using StackExchange.Redis;

namespace Hotel.Api.Services;

public interface IDistributedLockService
{
    Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan? expiry = null);
}
public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan? expiry = null)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:{key}";
        var token = Guid.NewGuid().ToString();
        var ttl = expiry ?? TimeSpan.FromSeconds(10);

        while (true)
        {
            var acquired = await db.StringSetAsync(lockKey, token, ttl, When.NotExists);

            if (acquired)
            {
                return new RedisLockHandle(db, lockKey, token);
            }

            await Task.Delay(50);
        }
    }

    private class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public RedisLockHandle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

            await _db.ScriptEvaluateAsync(script,
                new RedisKey[] { _key },
                new RedisValue[] { _token });
        }
    }
}