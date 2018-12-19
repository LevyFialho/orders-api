using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Messages;
#pragma warning disable S1066

namespace OrdersApi.Cqrs.Repository
{
    public class AggregateDataSource : IRepository
    {
        private bool _disposed = false;

        public const int SmallIntMaxValue = 32767;

        public IEventStorageProvider EventStorageProvider { get; }

        public ISnapshotStorageProvider SnapshotStorageProvider { get; }

        public IMessageBus MessageBus { get; }

        public AggregateDataSource(IEventStorageProvider eventStorageProvider, ISnapshotStorageProvider snapshotStorageProvider, IMessageBus eventPublisher)
        {
            EventStorageProvider = eventStorageProvider;
            SnapshotStorageProvider = snapshotStorageProvider;
            MessageBus = eventPublisher;
        }

        public virtual async Task<T> GetByIdAsync<T>(string aggregateKey) where T : AggregateRoot
        {
            T item = default(T);

            var isSnapshottable = typeof(ISnapshottable).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo());
            Snapshot snapshot = null;

            if ((isSnapshottable) && (SnapshotStorageProvider != null))
            {
                snapshot = await SnapshotStorageProvider.GetSnapshotAsync(typeof(T), aggregateKey);
            }

            if (snapshot != null)
            {
                item = ReflectionHelper.CreateInstance<T>();
                ((ISnapshottable)item).ApplySnapshot(snapshot);
                var events = await EventStorageProvider.GetEventsAsync(aggregateKey, snapshot.Version + 1, SmallIntMaxValue);
                item.LoadsFromHistory(events);
            }
            else
            {
                var events = (await EventStorageProvider.GetEventsAsync(aggregateKey, 0, SmallIntMaxValue)).ToList();

                if (events.Any())
                {
                    item = ReflectionHelper.CreateInstance<T>();
                    item.LoadsFromHistory(events);
                }
            }

            return item;
        }

        public virtual async Task<T> GetAsync<T>(string correlationKey, string applicationKey) where T : AggregateRoot
        {
            T item = default(T);


            var events = (await EventStorageProvider.GetEventsAsync(correlationKey, applicationKey, 0, SmallIntMaxValue)).ToList();

            if (events.Any())
            {
                item = ReflectionHelper.CreateInstance<T>();
                item.LoadsFromHistory(events);
            }


            return item;
        }

        public virtual async Task<List<IEvent>> GetAsync(string correlationKey, string applicationKey) 
        {

            var events = (await EventStorageProvider.GetEventsAsync(correlationKey, applicationKey, 0, SmallIntMaxValue)).ToList();
            return events;
        }

        public virtual async Task SaveAsync<T>(T aggregate) where T : AggregateRoot
        {
            if (aggregate.HasUncommittedChanges())
            {
                await CommitChanges(aggregate);
            }
        }

        private async Task CommitChanges(AggregateRoot aggregate)
        {
            var expectedVersion = aggregate.LastCommittedVersion;

            IEvent item = await EventStorageProvider.GetLastEventAsync(aggregate.AggregateKey);

            if ((item != null) && (expectedVersion == (int)AggregateRoot.StreamState.NoStream))
            {
                throw new AggregateCreationException($"Aggregate can't be created as it already exists with version {item.TargetVersion + 1}");
            }
            else if ((item != null) && ((item.TargetVersion + 1) != expectedVersion))
            {
                throw new ConcurrencyException($"Aggregate  has been modified externally and has an updated state. Can't commit changes.");
            }

            var changesToCommit = aggregate.GetUncommittedChanges().ToList();

            //perform pre commit actions
            foreach (var e in changesToCommit)
            {
                DoPreCommitTasks(e);
            }

            //CommitAsync events to storage provider
             await EventStorageProvider.CommitChangesAsync(aggregate);
             
            //Publish to event publisher asynchronously
            foreach (var e in changesToCommit)
            {
                await MessageBus.RaiseEvent(e);
            }

            //If the Aggregate implements snaphottable
            var snapshottable = aggregate as ISnapshottable;

            if ((snapshottable != null) && (SnapshotStorageProvider != null))
            {
                //Every N events we save a snapshot
                if (aggregate.CurrentVersion >= SnapshotStorageProvider.SnapshotFrequency &&
                        (
                            (changesToCommit.Count >= SnapshotStorageProvider.SnapshotFrequency) ||
                            (aggregate.CurrentVersion % SnapshotStorageProvider.SnapshotFrequency < changesToCommit.Count) ||
                            (aggregate.CurrentVersion % SnapshotStorageProvider.SnapshotFrequency == 0)
                        )
                    )
                {
                    await SnapshotStorageProvider.SaveSnapshotAsync(aggregate.GetType(), snapshottable.TakeSnapshot());
                }
            }

            aggregate.MarkChangesAsCommitted();
        }

        private static void DoPreCommitTasks(IEvent e)
        {
            e.EventCommittedTimestamp = DateTime.UtcNow;
        }

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [ExcludeFromCodeCoverage]
        /// <summary>
        /// Protected implementation of the Dispose pattern.
        /// </summary>
        /// <param name="disposing">Flag to indicate a disposing running in this instance</param>
        protected virtual void Dispose(bool disposing)
        {

            if (_disposed) { return; }

            if (disposing)
            {
                EventStorageProvider?.Dispose();
                SnapshotStorageProvider?.Dispose();
                MessageBus?.Dispose();
            }

            _disposed = true;
        }
    }
}
