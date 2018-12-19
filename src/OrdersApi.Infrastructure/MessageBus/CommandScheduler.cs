using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Domain.Commands;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.Settings;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;

namespace OrdersApi.Infrastructure.MessageBus
{
    [ExcludeFromCodeCoverage]
    public class CommandScheduler : ICommandScheduler
    {
        private readonly ICommandBus _bus; 

        public CommandScheduler(ICommandBus bus)
        {
            _bus = bus; 
        }

        public async Task RunNow<T>(T command) where T : ICommand
        {
            await _bus.Publish(command, DateTime.UtcNow);
        }

        public async Task RunDelayed<T>(TimeSpan span, T command) where T : ICommand
        {
            await _bus.Publish(command, DateTime.UtcNow.Add(span));
        }

    }
}
