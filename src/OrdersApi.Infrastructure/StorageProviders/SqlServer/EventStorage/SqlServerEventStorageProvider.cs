using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
#pragma warning disable 1998
#pragma warning disable CS1998

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class SqlServerEventStorageProvider : IEventStorageProvider
    {
        private readonly SqlEventStorageContext _context;
        private bool _disposed;

        public SqlServerEventStorageProvider(SqlEventStorageContext context)
        {
            _context = context;
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
                _context?.Dispose();
            }
            _disposed = true;
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateKey, int start, int count)
        {
            var listOfEventData = _context.Events.Where(x => x.AggregateKey == aggregateKey && x.TargetVersion >= start - 1);

            return listOfEventData.Select(x => x.DeserializeEvent()).OrderBy(x => x.TargetVersion).Take(count);
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateKey, int start, int count, Type aggregateType)
        {
            var listOfEventData = _context.Events.Where(x => x.AggregateKey == aggregateKey && x.TargetVersion >= start - 1).ToList();

            return listOfEventData.Select(x => x.GetPayload()).Where(x => x.AggregateType == aggregateType.AssemblyQualifiedName)
                .Select(x => x.DeserializeEvent()).OrderBy(x => x.TargetVersion).Take(count);
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(string correlationKey, string applicationKey, int start, int count)
        {
            var listOfEventData = _context.Events.Where(x => x.CorrelationKey == correlationKey && x.ApplicationKey == applicationKey
            && x.TargetVersion >= start - 1);

            return listOfEventData.Select(x => x.DeserializeEvent()).OrderBy(x => x.TargetVersion).Take(count);
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(string correlationKey, string applicationKey, int start, int count, Type aggregateType)
        {
            var listOfEventData = _context.Events.Where(x => x.CorrelationKey == correlationKey
            && x.ApplicationKey == applicationKey && x.TargetVersion >= start - 1).ToList();

            return listOfEventData.Select(x => x.GetPayload()).Where(x => x.AggregateType == aggregateType.AssemblyQualifiedName)
                .Select(x => x.DeserializeEvent()).OrderBy(x => x.TargetVersion).Take(count);
        }

        public async Task<IEvent> GetLastEventAsync(string aggregateKey)
        {
            var listOfEventData = _context.Events.Where(x => x.AggregateKey == aggregateKey);
            return listOfEventData.LastOrDefault()?.DeserializeEvent();
        }

        public async Task CommitChangesAsync(AggregateRoot aggregate)
        {
            var events = aggregate.GetUncommittedChanges().ToList();
            if (events.Any())
            {
                var eventsPersisted = await GetEventsAsync(aggregate.AggregateKey, 0, AggregateDataSource.SmallIntMaxValue);
                var foundVersion = eventsPersisted == null || !eventsPersisted.Any() ? -1 : eventsPersisted.Max(x => x.TargetVersion);

                if (foundVersion >= aggregate.CurrentVersion)
                    throw new ConcurrencyException("Concurrency exception, deprecated target version");

                foreach (var @event in events)
                {
                    _context.Events.Add(@event.ToEventData(aggregate.GetType()));
                }
                _context.SaveChanges();
            }
        }
    }
}
