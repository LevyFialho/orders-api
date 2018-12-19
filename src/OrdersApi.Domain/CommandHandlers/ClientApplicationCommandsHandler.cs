using System;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using MediatR;

namespace OrdersApi.Domain.CommandHandlers
{
    public class ClientApplicationCommandsHandler : CommandHandler, IRequestHandler<CreateClientApplication>,
        IRequestHandler<ActivateClientApplication>, IRequestHandler<RevokeClientApplicationCreation>, IRequestHandler<UpdateProductAccess>
    { 
        public ClientApplicationCommandsHandler(AggregateDataSource repository, IMessageBus bus) : base(repository, bus)
        { 
        }
        
        public async Task<Unit> Handle(CreateClientApplication request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return await Unit.Task;
            }
            await ValidateDuplicateCommand(request);

            var app = new ClientApplication(request.AggregateKey, request.CorrelationKey,  request.ExternalId, request.ApplicationKey, request.Name, request.SagaProcessKey);
            Repository.SaveAsync(app).Wait(CancellationToken.None);
            return await Unit.Task;
        }

        public async Task<Unit> Handle(ActivateClientApplication request, CancellationToken cancellationToken)
        {
            await ValidateDuplicateCommand(request);
            var app = await Repository.GetByIdAsync<ClientApplication>(request.AggregateKey);
            if (app != null)
            {
                app.Activate(request.CorrelationKey, request.ApplicationKey, request.SagaProcessKey);
                Repository.SaveAsync(app).Wait(CancellationToken.None);
            }
            return await Unit.Task;
        }

        public async Task<Unit> Handle(RevokeClientApplicationCreation request, CancellationToken cancellationToken)
        {
            await ValidateDuplicateCommand(request);
            var app = await Repository.GetByIdAsync<ClientApplication>(request.AggregateKey);
            if (app != null)
            {
                app.Reject(request.CorrelationKey, request.ApplicationKey, request.Reason, request.SagaProcessKey);
                Repository.SaveAsync(app).Wait(CancellationToken.None);
            }
            return await Unit.Task;
        }

        public async Task<Unit> Handle(UpdateProductAccess request, CancellationToken cancellationToken)
        {
            await ValidateDuplicateCommand(request);
            var app = await Repository.GetByIdAsync<ClientApplication>(request.AggregateKey);
            var product = await Repository.GetByIdAsync<Product>(request.ProductAggregateKey);
            if (product == null || product.Status != ProductStatus.Active)
            {
                throw new AggregateNotFoundException("Invalid ProductKey");
            }
            if (app != null)
            {
                app.UpdateProductAccess(request.CorrelationKey, request.ApplicationKey, new ProductAccess()
                {
                    CanQuery = request.CanQuery,
                    ProductAggregateKey = request.ProductAggregateKey,
                    CanCharge = request.CanCharge
                }, request.SagaProcessKey);
                Repository.SaveAsync(app).Wait(CancellationToken.None);
            }
            return await Unit.Task;
        }
    }
}
