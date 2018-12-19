using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.UnitTests.Cqrs.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Repository
{
    public class AggregateDataSourceTests
    {
        public class TestRepository : AggregateDataSource
        {
            public TestRepository(IEventStorageProvider eventStorageProvider, ISnapshotStorageProvider snapshotStorageProvider, IMessageBus eventPublisher) 
                : base(eventStorageProvider, snapshotStorageProvider, eventPublisher)
            {
            }
        }

        public class GetByIdAsyncTests
        {
            [Fact]
            public void GetByIdAsyncReturnsNothingTest()
            { 
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var aggregateId = Guid.NewGuid().ToString();
                var repository = new AggregateDataSource(eventStorageProvider.Object,snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetByIdAsync<TestAggregate>(aggregateId).Result;
                snapshotProvider.Verify(x => x.GetSnapshotAsync(typeof(TestAggregate), aggregateId), Times.Never);
                eventStorageProvider.Verify(x => x.GetEventsAsync(aggregateId, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.Null(aggregate);
            }

            [Fact]
            public void GetByIdAsyncReturnsAggregateTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var aggregateId = Guid.NewGuid().ToString();
                IEnumerable<IEvent> eventList = new List<IEvent>()
                {
                    new TestEvent()
                    {
                        AggregateKey = aggregateId,
                        TargetVersion = -1
                    }
                }.ToArray();
                eventStorageProvider.Setup(x => x.GetEventsAsync(aggregateId, 0, AggregateDataSource.SmallIntMaxValue))
                    .Returns(Task.FromResult(eventList));
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetByIdAsync<TestAggregate>(aggregateId).Result;
                snapshotProvider.Verify(x => x.GetSnapshotAsync(typeof(TestAggregate), aggregateId), Times.Never);
                eventStorageProvider.Verify(x => x.GetEventsAsync( aggregateId, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.NotNull(aggregate);
                Assert.Equal(aggregateId, aggregate.AggregateKey);
            }

            [Fact]
            public void GetSnapshottableByIdAsyncReturnsNothingTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var aggregateId = Guid.NewGuid().ToString();
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetByIdAsync<SnapshottableTestAggregate>(aggregateId).Result;
                snapshotProvider.Verify(x => x.GetSnapshotAsync(typeof(SnapshottableTestAggregate), aggregateId), Times.Once);
                eventStorageProvider.Verify(x => x.GetEventsAsync(aggregateId, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.Null(aggregate);
            }

            [Fact]
            public void GetSnapshottableByIdAsyncReturnsAggregateTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var aggregateId = Guid.NewGuid().ToString();
                var snapshotId = Guid.NewGuid().ToString();
                short version = 0;
                var snapshot = new Snapshot(snapshotId, aggregateId, version);
                snapshotProvider.Setup(x => x.GetSnapshotAsync(typeof(SnapshottableTestAggregate), aggregateId))
                    .Returns(Task.FromResult(snapshot));
           
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetByIdAsync<SnapshottableTestAggregate>(aggregateId).Result;
                snapshotProvider.Verify(x => x.GetSnapshotAsync(typeof(SnapshottableTestAggregate), aggregateId), Times.Once);
                eventStorageProvider.Verify(x => x.GetEventsAsync( aggregateId, 1, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.NotNull(aggregate);
                Assert.Equal(aggregateId, aggregate.AggregateKey);
            }
        }

        public class GetByCorrelationAndApplicationKeysTests
        {
            [Fact]
            public void ReturnsNothingTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var correlationKey = Guid.NewGuid().ToString();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetAsync<TestAggregate>(correlationKey, applicationKey).Result; 
                eventStorageProvider.Verify(x => x.GetEventsAsync(correlationKey, applicationKey, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.Null(aggregate);
            }

            [Fact]
            public void GetReturnsAggregateTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var correlationKey = Guid.NewGuid().ToString();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                IEnumerable<IEvent> eventList = new List<IEvent>()
                {
                    new TestEvent()
                    {
                        CorrelationKey = correlationKey,
                        ApplicationKey = applicationKey,
                        TargetVersion = -1
                    }
                }.ToArray();
                eventStorageProvider.Setup(x => x.GetEventsAsync(correlationKey, applicationKey, 0, AggregateDataSource.SmallIntMaxValue))
                    .Returns(Task.FromResult(eventList));
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetAsync<TestAggregate>(correlationKey, applicationKey).Result; 
                eventStorageProvider.Verify(x => x.GetEventsAsync(correlationKey, applicationKey, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.NotNull(aggregate); 
            }


            [Fact]
            public void GetListReturnsAggregateTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var correlationKey = Guid.NewGuid().ToString();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                IEnumerable<IEvent> eventList = new List<IEvent>()
                {
                    new TestEvent()
                    {
                        CorrelationKey = correlationKey,
                        ApplicationKey = applicationKey,
                        TargetVersion = -1
                    }
                }.ToArray();
                eventStorageProvider.Setup(x => x.GetEventsAsync(correlationKey, applicationKey, 0, AggregateDataSource.SmallIntMaxValue))
                    .Returns(Task.FromResult(eventList));
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var events = repository.GetAsync(correlationKey, applicationKey).Result;
                eventStorageProvider.Verify(x => x.GetEventsAsync(correlationKey, applicationKey, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.NotNull(events);
                Assert.Single(events);
            }
        }

        public class GetAsyncTests
        {
            [Fact]
            public void GetAsyncReturnsNothingTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var correlationId = Guid.NewGuid().ToString();
                var requesterId = "X";
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetAsync<TestAggregate>(correlationId, requesterId).Result; 
                eventStorageProvider.Verify(x => x.GetEventsAsync(correlationId, requesterId, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.Null(aggregate);
            }

            [Fact]
            public void GetByIdAsyncReturnsAggregateTest()
            {
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                var aggregateId = Guid.NewGuid().ToString();
                var correlationId = Guid.NewGuid().ToString();
                var requesterId = "X";
                IEnumerable<IEvent> eventList = new List<IEvent>()
                {
                    new TestEvent()
                    {
                        AggregateKey = aggregateId,
                        TargetVersion = -1
                    }
                }.ToArray();
                eventStorageProvider.Setup(x => x.GetEventsAsync(correlationId, requesterId, 0, AggregateDataSource.SmallIntMaxValue))
                    .Returns(Task.FromResult(eventList));
                var repository = new AggregateDataSource(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object);

                var aggregate = repository.GetAsync<TestAggregate>(correlationId, requesterId).Result; 
                eventStorageProvider.Verify(x => x.GetEventsAsync(correlationId, requesterId, 0, AggregateDataSource.SmallIntMaxValue), Times.Once);
                Assert.NotNull(aggregate);
                Assert.Equal(aggregateId, aggregate.AggregateKey);
            }
              
        }

        public class SaveAsynchTests
        {
            [Fact]
            public async void SaveAsyncNoChangesTest()
            {
                var aggregateMock = new Mock<AggregateRoot>(){ CallBase = true};
                aggregateMock.Setup(x => x.HasUncommittedChanges()).Returns(false);
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>(); 
                var repository = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object) {CallBase = true};
                await repository.Object.SaveAsync(aggregateMock.Object);
                eventStorageProvider.Verify(x => x.CommitChangesAsync(aggregateMock.Object), Times.Never);
            }

            [Fact]
            public async void SaveAsyncHasChangesNoConflictsTest()
            {
                var aggregateMock = new Mock<SnapshottableTestAggregate>() { CallBase = true };
                Snapshot snap = new Snapshot();
                aggregateMock.Setup(x => x.HasUncommittedChanges()).Returns(true);
                aggregateMock.Setup(x => x.TakeSnapshot()).Returns(snap);
                aggregateMock.Setup(x => x.CurrentVersion).Returns(10);
                var testEvent = new TestEvent();
                aggregateMock.Setup(x => x.GetUncommittedChanges()).Returns(new List<IEvent>(){ testEvent });
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                snapshotProvider.Setup(x => x.SnapshotFrequency).Returns(0);
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                var eventPublisher = new Mock<IMessageBus>();
                eventPublisher.Setup(x => x.RaiseEvent(testEvent)).Verifiable();
                var repository = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object) { CallBase = true };
                await repository.Object.SaveAsync(aggregateMock.Object);

                eventStorageProvider.Verify(x => x.GetLastEventAsync(aggregateMock.Object.AggregateKey), Times.Once);
                Assert.NotEqual(testEvent.EventCommittedTimestamp, DateTime.MinValue);
                eventStorageProvider.Verify(x => x.CommitChangesAsync(aggregateMock.Object), Times.Once);
                eventPublisher.Verify(x => x.RaiseEvent(It.IsAny<IEvent>()), Times.Once);
                aggregateMock.Verify(x => x.TakeSnapshot(), Times.Once);
                snapshotProvider.Verify(x => x.SaveSnapshotAsync(aggregateMock.Object.GetType(),snap), Times.Once);

            }

            [Fact]
            public void SaveAsyncAggregateCreationExceptionTest()
            {
                var aggregateMock = new Mock<SnapshottableTestAggregate>() { CallBase = true };
                aggregateMock.Setup(x => x.LastCommittedVersion).Returns((int)AggregateRoot.StreamState.NoStream);
                var aggregateId = Guid.NewGuid().ToString();
                aggregateMock.Setup(x => x.AggregateKey).Returns(aggregateId);
                aggregateMock.Setup(x => x.HasUncommittedChanges()).Returns(true); 
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                IEvent mockEvent = new TestEvent();
                eventStorageProvider
                    .Setup(x => x.GetLastEventAsync(aggregateMock.Object.AggregateKey))
                    .Returns(Task.FromResult(mockEvent));
                var eventPublisher = new Mock<IMessageBus>(); 
                var repository = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object) { CallBase = true };

                var saveAsync = new Func<Task>(async () => await repository.Object.SaveAsync(aggregateMock.Object));
                saveAsync.Should().Throw<AggregateCreationException>();

            }

            [Fact]
            public void SaveAsyncConcurrencyExceptionExceptionTest()
            {
                var aggregateMock = new Mock<SnapshottableTestAggregate>() { CallBase = true };
                aggregateMock.Setup(x => x.LastCommittedVersion).Returns((int)AggregateRoot.StreamState.HasStream);
                var aggregateId = Guid.NewGuid().ToString();
                aggregateMock.Setup(x => x.AggregateKey).Returns(aggregateId);
                aggregateMock.Setup(x => x.HasUncommittedChanges()).Returns(true);
                var snapshotProvider = new Mock<ISnapshotStorageProvider>();
                var eventStorageProvider = new Mock<IEventStorageProvider>();
                IEvent mockEvent = new TestEvent(){ TargetVersion = 1 };
                eventStorageProvider
                    .Setup(x => x.GetLastEventAsync(aggregateMock.Object.AggregateKey))
                    .Returns(Task.FromResult(mockEvent));
                var eventPublisher = new Mock<IMessageBus>();
                var repository = new Mock<AggregateDataSource>(eventStorageProvider.Object, snapshotProvider.Object, eventPublisher.Object) { CallBase = true };
                var saveAsync = new Func<Task>(async () => await repository.Object.SaveAsync(aggregateMock.Object));
                saveAsync.Should().Throw<ConcurrencyException>();
                 

            }
        }
    }
}
