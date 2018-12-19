using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;

namespace OrdersApi.Cqrs.Messages
{ 
    public interface IMessageBus: IDisposable
    { 
        Task SendCommand<T>(T command) where T : ICommand;
         
        Task RaiseEvent<T>(T @event) where T : IEvent;
    }
}
