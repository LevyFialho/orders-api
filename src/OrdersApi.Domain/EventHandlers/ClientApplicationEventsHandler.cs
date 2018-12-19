using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;

namespace OrdersApi.Domain.EventHandlers
{
    public class ClientApplicationEventsHandler : IEventHandler<ClientApplicationCreated>, 
                                                  IEventHandler<ClientApplicationActivated>,
                                                  IEventHandler<ClientApplicationCreationRevoked>, 
                                                  IEventHandler<ProductAccessUpdated>
    {
        protected readonly IQueryableRepository<ClientApplicationProjection> Repository;
        protected readonly ICommandScheduler CommandScheduler;

        public ClientApplicationEventsHandler(IQueryableRepository<ClientApplicationProjection> repository, ICommandScheduler commandScheduler)
        {
            Repository = repository;
            CommandScheduler = commandScheduler;
        }

        public async Task Handle(ClientApplicationCreated notification, CancellationToken cancellationToken)
        {
            var existingProjectionSpecification = ClientApplicationSpecifications.VerifyAlreadyCreated(notification.ExternalKey, notification.AggregateKey);

            var alreadyCreated = Repository.GetFiltered(existingProjectionSpecification).Any();

            if (alreadyCreated)
            {
                await CommandScheduler.RunNow(new RevokeClientApplicationCreation(notification.AggregateKey,
                                                                                  IdentityGenerator.NewSequentialIdentity(), 
                                                                                  notification.ApplicationKey, 
                                                                                  "Duplicated ExternalKey", 
                                                                                  notification.SagaProcessKey));
            }
            else
            {
                var existingProjection = Repository.Get(notification.AggregateKey);

                if (existingProjection == null)
                {
                    var projection = new ClientApplicationProjection(notification);

                    await Repository.AddAynsc(projection); //Add aggregate projection to Mongo
                }

                await CommandScheduler.RunNow(new ActivateClientApplication(notification.AggregateKey,
                                                                            IdentityGenerator.NewSequentialIdentity(), 
                                                                            notification.ApplicationKey, 
                                                                            notification.SagaProcessKey));
            }
        }

        public async Task Handle(ClientApplicationActivated notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);
            if (existingProjection != null)
            {
                existingProjection.Update(notification);
                await Repository.UpdateAsync(existingProjection); 
            }
        }

        public async Task Handle(ClientApplicationCreationRevoked notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);
            if (existingProjection != null)
            {
                existingProjection.Update(notification);
                await Repository.UpdateAsync(existingProjection);
            }
        }

        public async Task Handle(ProductAccessUpdated notification, CancellationToken cancellationToken)
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
