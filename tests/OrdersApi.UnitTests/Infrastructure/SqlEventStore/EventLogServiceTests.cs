using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoFixture;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventLog.Services;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using OrdersApi.UnitTests.Cqrs.Events;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
#pragma warning disable CS0618

namespace OrdersApi.UnitTests.Infrastructure.SqlEventStore
{
    public class EventLogServiceTests
    {

        [Fact]
        public async void MasrkAsPublishedTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqlEventStorageContext>();
            optionsBuilder.UseInMemoryDatabase();

            var context = new SqlEventStorageContext(optionsBuilder.Options);
            var evt = new TestEvent().ToEventData(typeof(TestEvent));
            evt.State = EventState.NotPublished;
            context.Events.Add(evt);
            context.SaveChanges();
            var logService = new EventLogService(context);

            await logService.MarkEventAsPublishedAsync(evt);

            Assert.Equal(EventState.Published, evt.State);
            Assert.Equal(1, evt.TimesSent);
        }

        [Fact]
        public void GetUnpublishedEventsTests()
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqlEventStorageContext>();
            optionsBuilder.UseInMemoryDatabase();

            var context = new SqlEventStorageContext(optionsBuilder.Options);
            var evt = new TestEvent().ToEventData(typeof(TestEvent));
            evt.State = EventState.NotPublished;
            context.Events.Add(evt);
            context.SaveChanges();
            var logService = new EventLogService(context);

            var unpublished = logService.GetUnpublishedEvents();

            Assert.NotNull(unpublished);
            Assert.Single(unpublished);
        }
    }
}
