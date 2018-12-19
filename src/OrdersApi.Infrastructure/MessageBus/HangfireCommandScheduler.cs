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
    public class HangfireCommandScheduler : ICommandScheduler
    {
        private readonly IMessageBus _bus;
        private readonly int _retryCount;
        private readonly ILogger<HangfireCommandScheduler> _logger;
        public HangfireCommandScheduler(IMessageBus bus, IConfiguration configuration, ILogger<HangfireCommandScheduler> logger)
        {
            _bus = bus;
            _logger = logger;
            var settings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
            _retryCount = settings.EnqueueJobRetryCount;
        }


        public Task RunNow<T>(T command) where T : ICommand
        {
            var policy = Policy.Handle<Exception>() 
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, ex.ToString());
                });
            policy.Execute(() =>
            {
                string jobId = BackgroundJob.Enqueue(() => SendCommand(GetDescription(command), command));
                if (string.IsNullOrWhiteSpace(jobId))
                    throw new CommandExecutionFailedException("Could not create hangfire job");
            });
            return Task.CompletedTask;
        }

        public Task RunDelayed<T>(TimeSpan span, T command) where T : ICommand
        {
            var policy = Policy.Handle<Exception>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, ex.ToString());
                });
            policy.Execute(() =>
            {
                string jobId = BackgroundJob.Schedule(() => SendCommand(GetDescription(command), command), span);
                if (string.IsNullOrWhiteSpace(jobId))
                    throw new CommandExecutionFailedException("Could not create hangfire job");
            });
            return Task.CompletedTask;
        } 

        private string GetDescription<T>(T command) where T : ICommand
        {
            return command.GetType().Name + " " + command.AggregateKey;
        }

        [DisplayName("{0}")]
        public void SendCommand<T>(string description,  T command) where T : ICommand
        {
            try
            {
                _bus.SendCommand(command).Wait(CancellationToken.None);
            }
            catch (DuplicateException ex)
            {
                _logger.LogWarning(ex, ex.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.ToString());
                throw;
            }
        }
    }
}
