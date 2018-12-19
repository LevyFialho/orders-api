using System;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate; 
using MediatR;
using Microsoft.Extensions.Logging;

namespace OrdersApi.Domain.CommandHandlers
{
    public class ReversalCommandsHandler : CommandHandler, IRequestHandler<CreateChargeReversal>, IRequestHandler<ProcessAcquirerAccountReversal>,
        IRequestHandler<VerifyReversalSettlement>
    {
        protected readonly ILegacyApiService AcquirerApiService;
        protected readonly ILogger<ReversalCommandsHandler> Logger;

        public ReversalCommandsHandler(AggregateDataSource repository, IMessageBus bus, ILegacyApiService acquirerApiService, ILogger<ReversalCommandsHandler> logger) : base(repository, bus)
        {
            AcquirerApiService = acquirerApiService;
            Logger = logger;
        }
        
        public async Task<Unit> Handle(CreateChargeReversal request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return await Unit.Task;
            }
            await ValidateDuplicateCommand(request);

            var order = await Repository.GetByIdAsync<Charge>(request.AggregateKey);

            if(order == null)
                throw new AggregateNotFoundException(request.AggregateKey);

            if(!order.CanRevert(request.Amount))
                throw new CommandExecutionFailedException("Cannot revert order. Verify the order status and reversal amount.");

            order.Revert(request.CorrelationKey, request.ApplicationKey, request.SagaProcessKey, request.Amount, request.ReversalKey);

            Repository.SaveAsync(order).Wait(CancellationToken.None);

            return await Unit.Task;
        }

        public async Task<Unit> Handle(ProcessAcquirerAccountReversal request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return await Unit.Task;
            }
            try
            {
                await ValidateDuplicateCommand(request);
            }
            catch (DuplicateException e)
            {
                Logger.LogInformation(e.ToString());
                return await Unit.Task;
            }
            var order = await Repository.GetByIdAsync<Charge>(request.AggregateKey);
            if (order != null)
            {
                order.SendReversalToAcquirer(request.CorrelationKey, request.ApplicationKey,
                    request.SagaProcessKey, request.ReversalKey, AcquirerApiService);
                Repository.SaveAsync(order).Wait(CancellationToken.None);
            }

            return await Unit.Task;
        }

        public async Task<Unit> Handle(VerifyReversalSettlement request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return await Unit.Task;
            }
            try
            {
                await ValidateDuplicateCommand(request);
            }
            catch (DuplicateException e)
            {
                Logger.LogInformation(e.ToString());
                return await Unit.Task;
            }
            var order = await Repository.GetByIdAsync<Charge>(request.AggregateKey);
            if (order != null)
            {
                order.VerifyReversalSettlement(request.CorrelationKey, request.ApplicationKey,
                    request.SagaProcessKey, request.ReversalKey, AcquirerApiService);
                Repository.SaveAsync(order).Wait(CancellationToken.None);
            }
            return await Unit.Task;
        }

    }
}
