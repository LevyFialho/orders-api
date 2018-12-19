using System;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.Cqrs.Repository
{
    public interface ISnapshotStorageProvider : IDisposable
    {
        int SnapshotFrequency { get; }
        Task<Snapshot> GetSnapshotAsync(Type aggregateType, string aggregateId); 
        Task SaveSnapshotAsync(Type aggregateType, Snapshot snapshot);
        Task<Projection> GetProjectionSnapshotAsync(Type projectionType, string projectionSnapshotKey);
        Task SaveProjectionSnapshotAsync(Projection snapshot);
    }
}
