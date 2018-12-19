using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
#pragma warning disable 1998
#pragma warning disable CS1998

namespace OrdersApi.Infrastructure.StorageProviders.Redis
{
    public class RedisSnapshotStorageProvider : ISnapshotStorageProvider
    {
        public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public int SnapshotFrequency => settings.SnapshotFrequency;

        private readonly RedisSettings settings;
        private IDatabase database;
        private bool _disposed;

        public RedisSnapshotStorageProvider(ConnectionMultiplexer connectionMultiplexer, IOptions<RedisSettings> options)
        {
            this.settings = options.Value;
            this.database = connectionMultiplexer.GetDatabase();
        }

        public async Task<Projection> GetProjectionSnapshotAsync(Type projectionType, string projectionSnapshotKey)
        {
            Projection snapshot = null;

            var strSnapshot = await database.StringGetAsync(projectionType.Name + projectionSnapshotKey);

            if (!strSnapshot.IsNullOrEmpty)
            {
                snapshot = (Projection)JsonConvert.DeserializeObject(strSnapshot, projectionType, JsonSerializerSettings);
            }

            return snapshot;
        }

        public async Task<Snapshot> GetSnapshotAsync(Type aggregateType, string aggregateId)
        {
            Snapshot snapshot = null;

            var strSnapshot = await database.StringGetAsync(aggregateId);

            if (!strSnapshot.IsNullOrEmpty)
            {
                snapshot = JsonConvert.DeserializeObject<Snapshot>(strSnapshot, JsonSerializerSettings);
            }

            return snapshot;
        }

        public async Task SaveProjectionSnapshotAsync(Projection snapshot)
        {
            var strSnapshot = JsonConvert.SerializeObject(snapshot, JsonSerializerSettings);

            var redisKey = snapshot.GetType().Name + snapshot.SnapshotProjectionKey;

            database.StringSet(key: redisKey, 
                                          value: strSnapshot, 
                                          expiry: TimeSpan.FromMinutes(settings.SnapshotMinutesToExpire));
        }

        public async Task SaveSnapshotAsync(Type aggregateType, Snapshot snapshot)
        {
            var strSnapshot = JsonConvert.SerializeObject(snapshot, JsonSerializerSettings);

             database.StringSet(key: snapshot.AggregateKey, 
                                          value: strSnapshot, 
                                          expiry: TimeSpan.FromMinutes(settings.SnapshotMinutesToExpire));
        } 

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [ExcludeFromCodeCoverage]
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing)
            {
                database = null;
            }

            _disposed = true;
        }
    }
}
