namespace Hotel_Booking.Services
{
    using System.Text.Json;
    using StackExchange.Redis;

    namespace Hotel_Booking.Services
    {
        public class CacheService : ICacheService
        {
         

            // redis store Variables as Camel Case not Pascal as c# so we use this line
            // case not sensitive
            private static readonly JsonSerializerOptions _jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true
            };

            private readonly IConnectionMultiplexer _redis;
            private readonly IDatabase _database;

            public CacheService(IConnectionMultiplexer redis)
            {
                _redis = redis;
                _database = redis.GetDatabase();
            }

            public async Task<T?> GetAsync<T>(string key)
            {
                var value = await _database.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                    return default;

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }

            public async Task SetAsync<T>(
                string key,
                T value,
                TimeSpan expiration)
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);

                await _database.StringSetAsync(
                    key,
                    json,
                    expiration);
            }
             
            //remove one item ( cache)
            public async Task RemoveAsync(string key)
            {
                await _database.KeyDeleteAsync(key);
            }

            // remove pattern of cache used where is pagination there 
            public async Task RemoveByPatternAsync(string pattern)
            {
                var endpoints = _redis.GetEndPoints();

                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);

                    var keys = server
                        .Keys(pattern: pattern)
                        .ToArray();

                    if (keys.Length > 0)
                    {
                        await _database.KeyDeleteAsync(keys);
                    }
                }
            }

        }
    }
}
