using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CS1998
namespace OrdersApi.Infrastructure.StorageProviders.InMemory
{
    public class InMemorySnapshotStorageProvider : ISnapshotStorageProvider
    {
        private static ConcurrentDictionary<string, string> projections = new ConcurrentDictionary<string, string>();

        public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public int SnapshotFrequency => settings.SnapshotFrequency;

        private readonly RedisSettings settings;
        private bool _disposed;

        public InMemorySnapshotStorageProvider(IOptions<RedisSettings> options)
        {
            this.settings = options.Value;
        }

        public Task<Projection> GetProjectionSnapshotAsync(Type projectionType, string projectionSnapshotKey)
        {
            Projection snapshot = null;

            string strSnapshot;

            projections.TryGetValue(projectionType.Name + projectionSnapshotKey, out strSnapshot);

            if (!string.IsNullOrEmpty(strSnapshot))
            {
                snapshot = (Projection)JsonConvert.DeserializeObject(strSnapshot, projectionType, JsonSerializerSettings);
            }

            return Task.FromResult(snapshot);
        }

        public async Task<Snapshot> GetSnapshotAsync(Type aggregateType, string aggregateId)
        {
            Snapshot snapshot = null;

            string strSnapshot;

            projections.TryGetValue(aggregateId, out strSnapshot);

            if (!string.IsNullOrEmpty(strSnapshot))
            {
                snapshot = JsonConvert.DeserializeObject<Snapshot>(strSnapshot, JsonSerializerSettings);
            }

            return snapshot;
        }

        public async Task SaveProjectionSnapshotAsync(Projection snapshot)
        {
            var strSnapshot = JsonConvert.SerializeObject(snapshot, JsonSerializerSettings);

            var redisKey = snapshot.GetType().Name + snapshot.SnapshotProjectionKey;

            projections.TryAdd(key: redisKey,
                               value: strSnapshot);
        }

        public async Task SaveSnapshotAsync(Type aggregateType, Snapshot snapshot)
        {
            var strSnapshot = JsonConvert.SerializeObject(snapshot, JsonSerializerSettings);

            projections.TryAdd(key: snapshot.AggregateKey,
                               value: strSnapshot);
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

            _disposed = true;
        }
    }
}
