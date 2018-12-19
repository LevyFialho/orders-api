using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using Microsoft.EntityFrameworkCore; 

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventLog.Services
{
    public class EventLogService : IEventLogService
    {
        private readonly SqlEventStorageContext _context;

        public EventLogService(SqlEventStorageContext context)
        {
            _context = context;
        }

        public IEnumerable<IEvent> GetUnpublishedEvents()
        {
            var events = _context.Events.Where(x => x.State != EventState.Published).OrderBy(e => e.EventCommittedTimestamp).Take(1000);
            return events?.Select(x => x.DeserializeEvent()).ToList();
        }
         
        public Task MarkEventAsPublishedAsync(IEvent @event)
        {
            var eventLogEntry = _context.Events.FirstOrDefault(ie => ie.EventKey == @event.EventKey);
            if(eventLogEntry == null)
                return Task.FromException(new Exception("Event not found in event log"));
            eventLogEntry.TimesSent++;
            eventLogEntry.State = EventState.Published; 
            _context.Events.Update(eventLogEntry);
            _context.SaveChanges();
            return Task.CompletedTask;
        }
    }
}
