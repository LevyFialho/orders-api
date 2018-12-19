using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Messages;
using Hangfire;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    public class EventLogPublisher: IEventLogPublisher
    {
        private readonly IEventLogService _eventLogService;

        private readonly IMessageBus _bus;

        private bool _hasEventsToProcess = true;

        public EventLogPublisher(IEventLogService eventLogService, IMessageBus bus)
        {
            _eventLogService = eventLogService;
            _bus = bus;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 30 * 60)]
        public async Task ProcessUnpublishedEvents()
        {
            while (_hasEventsToProcess)
            {
                var unpublished = _eventLogService.GetUnpublishedEvents();
                if (unpublished == null || !unpublished.Any())
                {
                    _hasEventsToProcess = false;
                    return;
                }
                foreach (var @event in unpublished)
                {
                    await _bus.RaiseEvent(@event);
                    await _eventLogService.MarkEventAsPublishedAsync(@event);
                }
            }
        }
    }
}
