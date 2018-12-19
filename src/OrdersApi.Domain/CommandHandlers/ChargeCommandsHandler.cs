using System;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate; 
using MediatR;
using Microsoft.Extensions.Logging;

namespace OrdersApi.Domain.CommandHandlers
{
    public class ChargeCommandsHandler : CommandHandler, IRequestHandler<CreateAcquirerAccountCharge>,
        IRequestHandler<SendChargeToAcquirer>, IRequestHandler<ExpireCharge>, IRequestHandler<VerifyAcquirerSettlement>
    {
        protected readonly IAcquirerApiService AcquirerApiService;
        protected readonly ILogger<ChargeCommandsHandler> Logger;

        public ChargeCommandsHandler(AggregateDataSource repository, IMessageBus bus, IAcquirerApiService acquirerApiService, ILogger<ChargeCommandsHandler> logger) : base(repository, bus)
        {
            AcquirerApiService = acquirerApiService;
            Logger = logger;
        }
        
        public async Task<Unit> Handle(CreateAcquirerAccountCharge request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return await Unit.Task;
            }
            await ValidateDuplicateCommand(request); 

            var order = new Charge(request.AggregateKey, request.CorrelationKey, request.ApplicationKey,
                request.SagaProcessKey, request.OrderDetails, new PaymentMethodData(request.AcquirerAccount));

            Repository.SaveAsync(order).Wait(CancellationToken.None);

            return await Unit.Task;
        }

        public async Task<Unit> Handle(VerifyAcquirerSettlement request, CancellationToken cancellationToken)
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
            if (order != null && order.CanVerifySettlement)
            {
                order.VerifySettlement(request.CorrelationKey, request.ApplicationKey,
                    request.SagaProcessKey, AcquirerApiService);
                Repository.SaveAsync(order).Wait(CancellationToken.None);
            }
            return await Unit.Task;
        }
        public async Task<Unit> Handle(SendChargeToAcquirer request, CancellationToken cancellationToken)
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
            var order = await  Repository.GetByIdAsync<Charge>(request.AggregateKey);
            if (order != null)
            {
                order.SendToAcquirer(request.CorrelationKey, request.ApplicationKey,
                    request.SagaProcessKey, AcquirerApiService);
                Repository.SaveAsync(order).Wait(CancellationToken.None);
            }
            
            return await Unit.Task;
        }

        public async Task<Unit> Handle(ExpireCharge request, CancellationToken cancellationToken)
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
                order.Expire(request.CorrelationKey, request.ApplicationKey,
                    request.SagaProcessKey);
                Repository.SaveAsync(order).Wait(CancellationToken.None);
            }

            return await Unit.Task;
        }

 
    }
}
