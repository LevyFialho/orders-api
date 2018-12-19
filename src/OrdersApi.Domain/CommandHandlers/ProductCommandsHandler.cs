using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Commands.Product;
using OrdersApi.Domain.Model.ProductAggregate;
using MediatR;

namespace OrdersApi.Domain.CommandHandlers
{
    public class ProductCommandsHandler : CommandHandler, IRequestHandler<CreateProduct>,
                                                          IRequestHandler<ActivateProduct>,
                                                          IRequestHandler<RevokeProductCreation>, IRequestHandler<UpdateProductAcquirerConfiguration>
    { 
        public ProductCommandsHandler(AggregateDataSource repository, IMessageBus bus) : base(repository, bus)
        { 
        }
        
        public async Task<Unit> Handle(CreateProduct request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);

                return await Unit.Task;
            }

            await ValidateDuplicateCommand(request);

            var product = new Product(request.AggregateKey, request.CorrelationKey, request.ExternalId, request.ApplicationKey, request.Name, request.SagaProcessKey);

            Repository.SaveAsync(product).Wait(CancellationToken.None);

            return await Unit.Task;
        }

        public async Task<Unit> Handle(ActivateProduct request, CancellationToken cancellationToken)
        {
            await ValidateDuplicateCommand(request);

            var app = await Repository.GetByIdAsync<Product>(request.AggregateKey);

            if (app != null)
            {
                app.Activate(request.CorrelationKey, request.ApplicationKey, request.SagaProcessKey);

                Repository.SaveAsync(app).Wait(CancellationToken.None);
            }

            return await Unit.Task;
        }

        public async Task<Unit> Handle(RevokeProductCreation request, CancellationToken cancellationToken)
        {
            await ValidateDuplicateCommand(request);

            var app = await Repository.GetByIdAsync<Product>(request.AggregateKey);

            if (app != null)
            {
                app.RevokeCreation(request.CorrelationKey, request.ApplicationKey, request.Reason, request.SagaProcessKey);

                Repository.SaveAsync(app).Wait(CancellationToken.None);
            }

            return await Unit.Task;
        }

        public async Task<Unit> Handle(UpdateProductAcquirerConfiguration request, CancellationToken cancellationToken)
        {
            await ValidateDuplicateCommand(request);

            var app = await Repository.GetByIdAsync<Product>(request.AggregateKey);

            if (app != null)
            {
                app.SetAcquirerConfiguration(request.CorrelationKey, request.ApplicationKey, request.SagaProcessKey, request.Configuration);

                Repository.SaveAsync(app).Wait(CancellationToken.None);
            }

            return await Unit.Task;
        }
    }
}
