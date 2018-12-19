using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Commands.Product;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;

namespace OrdersApi.Domain.EventHandlers
{
    public class ProductEventsHandler : IEventHandler<ProductCreated>,
                                        IEventHandler<ProductActivated>,
                                        IEventHandler<ProductCreationRevoked>,
                                        IEventHandler<ProductAcquirerConfigurationUpdated>
    {
        protected readonly IQueryableRepository<ProductProjection> Repository;
        protected readonly ICommandScheduler CommandScheduler;

        public ProductEventsHandler(IQueryableRepository<ProductProjection> repository, ICommandScheduler commandScheduler)
        {
            Repository = repository;
            CommandScheduler = commandScheduler;
        }

        public async Task Handle(ProductCreated notification, CancellationToken cancellationToken)
        { 
            var existingProjection = Repository.Get(notification.AggregateKey);
            
            if (existingProjection == null)
            {
                var existingProductSpecification = ProductSpecifications.ProjectionByExternalKey(notification.ExternalKey);
                var existingProduct = Repository.GetFiltered(existingProductSpecification).FirstOrDefault();
                if (existingProduct == null)
                {
                    var projection = new ProductProjection(notification);

                    await Repository.AddAynsc(projection); //Add aggregate projection to Mongo

                    await CommandScheduler.RunNow(new ActivateProduct(notification.AggregateKey,
                        IdentityGenerator.NewSequentialIdentity(), notification.ApplicationKey, notification.SagaProcessKey));
                }
                else
                {
                    await CommandScheduler.RunNow(new RevokeProductCreation(notification.AggregateKey,
                        IdentityGenerator.NewSequentialIdentity(), notification.ApplicationKey, "Duplicated ExternalKey", notification.SagaProcessKey));
                }
            }
        }

        public async Task Handle(ProductActivated notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);
            if (existingProjection != null)
            {
                existingProjection.Update(notification);
                await Repository.UpdateAsync(existingProjection);
            }
        }

        public async Task Handle(ProductCreationRevoked notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);

            if (existingProjection != null)
            {
                existingProjection.Update(notification);
                await Repository.UpdateAsync(existingProjection);
            }
        }

        public async Task Handle(ProductAcquirerConfigurationUpdated notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);

            if (existingProjection != null)
            {
                existingProjection.Update(notification);
                await Repository.UpdateAsync(existingProjection);
            }
        }
    }
}
