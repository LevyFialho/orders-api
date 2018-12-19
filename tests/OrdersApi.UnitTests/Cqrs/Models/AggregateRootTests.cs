using System;
using System.Collections.Generic;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Models
{
    public class AggregateRootTests
    {
        [Fact]
        public void Int64HashKeyTest()
        {
            var idMap = new HashSet<string>();
            var aggregate = new Mock<AggregateRoot>() { CallBase = true };
            var hashKeyMap = new HashSet<long>();

            for (int i = 0; i < 100; i++) 
            {
                aggregate.Object.AggregateKey = IdentityGenerator.NewSequentialIdentity();
                idMap.Add(aggregate.Object.AggregateKey); 
                hashKeyMap.Add(aggregate.Object.GetInt64HashCode());
            }
        }

        [Fact]
        public void GetAbsoluteInt64HashCode()
        {
            var idMap = new HashSet<string>();
            var aggregate = new Mock<AggregateRoot>() { CallBase = true };
            var hashKeyMap = new HashSet<long>();

            for (int i = 0; i < 100; i++)
            {
                aggregate.Object.AggregateKey = IdentityGenerator.NewSequentialIdentity();
                idMap.Add(aggregate.Object.AggregateKey);
                var hash = aggregate.Object.GetAbsoluteInt64HashCode();
                Assert.True(hash >= 0);
                hashKeyMap.Add(hash);
            }
        } 

        [Fact]
        public void CurrentVersionTest()
        {
            var aggregate = new TestAggregate();
            Assert.Equal((int)AggregateRoot.StreamState.NoStream, aggregate.CurrentVersion);
        }

        [Fact]
        public void LastCommitedVersionTest()
        {
            var aggregate = new TestAggregate();
            Assert.Equal((int)AggregateRoot.StreamState.NoStream, aggregate.LastCommittedVersion);
        }

        [Fact]
        public void GetStreamStateTest()
        {
            var aggregate = new TestAggregate();
            Assert.Equal(AggregateRoot.StreamState.NoStream, aggregate.GetStreamState());
            aggregate.SetCurrentVersion(1);
            Assert.Equal(AggregateRoot.StreamState.HasStream, aggregate.GetStreamState());
        }
         
        [Fact]
        public void HasUncommittedChangesTest()
        {
            var aggregate = new TestAggregate();
            Assert.False(aggregate.HasUncommittedChanges());
            Action action = () => aggregate.LoadsFromHistory(new List<IEvent>() { new TestEvent() });
            action.Should().Throw<AggregateStateMismatchException>();
            var e = new TestEvent() { TargetVersion = aggregate.CurrentVersion };
            aggregate = new TestAggregate(e);
            Assert.True(aggregate.HasUncommittedChanges());
        }

        [Fact]
        public void GetUncommittedChangesTest()
        {
            var aggregate = new TestAggregate();
            Assert.Empty(aggregate.GetUncommittedChanges());
            Assert.NotNull(aggregate.GetUncommittedChanges());
            var e = new TestEvent() { TargetVersion = aggregate.CurrentVersion };
            aggregate = new TestAggregate(e);
            Assert.Single(aggregate.GetUncommittedChanges());
        }

        [Fact]
        public void MarkChangesAsCommittedTest()
        {
            var e = new TestEvent() { TargetVersion = -1 };
            var aggregate = new TestAggregate(e);
            aggregate.MarkChangesAsCommitted();
            Assert.Empty(aggregate.GetUncommittedChanges());
            Assert.False(aggregate.HasUncommittedChanges());
        }

        [Fact]
        public void LoadsFromHistoryTest()
        {
            var aggregate = new TestAggregate();
            var e = new TestEvent();
            Action action = () => aggregate.LoadsFromHistory(new List<IEvent>() { e });
            action.Should().Throw<AggregateStateMismatchException>();
            e.TargetVersion = aggregate.CurrentVersion;
            e.AggregateKey = aggregate.AggregateKey;
            aggregate.LoadsFromHistory(new List<IEvent>() { e });
            Assert.Equal(0, aggregate.CurrentVersion);

        }
    }
}
