using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using MediatR;

namespace OrdersApi.Infrastructure.MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly IMediator _mediator;
        private readonly IEventBus _eventBus; 
        private readonly IEventLogService _eventLogService;
        private bool _disposed;

        public MessageBus(IMediator mediator,  IEventBus eventBus = null, IEventLogService eventLogService = null)
        {
            _mediator = mediator;
            _eventBus = eventBus; 
            _eventLogService = eventLogService;
        }
         

        public Task SendCommand<T>(T command) where T : ICommand
        { 
            return _mediator.Send(command);
        } 
        public Task RaiseEvent<T>(T @event) where T : IEvent
        {
            if (_eventBus != null)
                _eventBus.Publish(@event);
            else
                _mediator.Publish(@event);

            return _eventLogService != null ? _eventLogService.MarkEventAsPublishedAsync(@event) : Task.CompletedTask;
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
