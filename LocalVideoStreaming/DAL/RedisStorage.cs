using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalVideoStreaming.DAL
{
	public class TrackTimeModel
	{
		public string Path { get; set; }
		public int Sec { get; set; }
		public TimeSpan? Expiry { get; set; }
	}

	public interface IRedisStorage
	{
		bool IsAvailable();
		Task TrackVideoTimeAsync(TrackTimeModel model);
		Task<IEnumerable<TrackTimeModel>> GetListOfVideoTimeAsync();
		Task ClearListOfVideoTimeAsync();
	}

	public class RedisStorage : IRedisStorage
	{
		private const string RedisKeyPrefix = "videoTime_";
		private const string RedisConnectionStringKey = "RedisConnectionString";
		private const string RedisServerName = "RedisServerName";
		private ConnectionMultiplexer _redis;

		public IConfiguration Configuration { get; }

		public RedisStorage(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public bool IsAvailable()
		{
			try
			{
				if (_redis == null)
					_redis = ConnectionMultiplexer.Connect(Configuration.GetValue<string>(RedisConnectionStringKey));

				// Test the connection.
				_redis.GetDatabase().StringGet("test_connection");

				return _redis.IsConnected;
			}
			catch (RedisException)
			{
				_redis = null; // force reconnect next time.
				return false;
			}
			catch (RedisTimeoutException)
			{
				_redis = null; // force reconnect next time.
				return false;
			}
		}

		public Task TrackVideoTimeAsync(TrackTimeModel model)
		{
			return _redis.GetDatabase().StringSetAsync(RedisKeyPrefix + model.Path, JsonConvert.SerializeObject(model), TimeSpan.FromDays(90));
		}

		public async Task<IEnumerable<TrackTimeModel>> GetListOfVideoTimeAsync()
		{
			var redisConnectionString = Configuration.GetValue<string>(RedisConnectionStringKey);
			var redisServerName = Configuration.GetValue<string>(RedisServerName);
			var keys = _redis.GetServer(redisServerName).Keys(pattern: RedisKeyPrefix + "*").Select(x => x.ToString());

			TrackTimeModel[] list;
			int maxParallelOperation = 10;
			using (var semaphore = new SemaphoreSlim(maxParallelOperation))
			{
				var tasks = keys.Select(async (key) =>
				{
					await semaphore.WaitAsync();
					try
					{
						var redisValueWithExpiry = await _redis.GetDatabase().StringGetWithExpiryAsync(key);
						var model = JsonConvert.DeserializeObject<TrackTimeModel>(redisValueWithExpiry.Value.ToString());
						model.Expiry = redisValueWithExpiry.Expiry;
						return model;
					}
					finally
					{
						semaphore.Release();
					}
				}).ToArray();

				list = await Task.WhenAll(tasks);
			}

			return list.OrderByDescending(x => x.Expiry.HasValue ? x.Expiry.Value.Ticks : 0);
		}

		public async Task ClearListOfVideoTimeAsync()
		{
			var redisConnectionString = Configuration.GetValue<string>(RedisConnectionStringKey);
			var keys = _redis.GetServer(redisConnectionString).Keys(pattern: RedisKeyPrefix + "*").Select(x => x.ToString());

			int maxParallelOperation = 10;
			using (var semaphore = new SemaphoreSlim(maxParallelOperation))
			{
				var tasks = keys.Select(async (key) =>
				{
					await semaphore.WaitAsync();
					try
					{
						await _redis.GetDatabase().KeyDeleteAsync(key);
					}
					finally
					{
						semaphore.Release();
					}
				}).ToArray();

				await Task.WhenAll(tasks);
			}
		}
	}
}
