using System;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using MediatR;

namespace OrdersApi.Infrastructure.MessageBus.Abstractions
{
    public interface ICommandBus
    {
        Task Publish(ICommand command, DateTime? scheduledEnqueueTimeUtc = null);

        void Subscribe<T, TH>()
            where T : ICommand
            where TH : IRequestHandler<T>; 

        void Unsubscribe<T, TH>()
            where TH : IRequestHandler<T>
            where T : ICommand;
    }
}
