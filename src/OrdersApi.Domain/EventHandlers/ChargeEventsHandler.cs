using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using OrdersApi.Domain.IntegrationServices;

#pragma warning disable S927
#pragma warning disable S1121

namespace OrdersApi.Domain.EventHandlers
{
    public class ChargeEventsHandler : IEventHandler<ChargeCreated>,
                                        IEventHandler<ChargeExpired>,
                                        IEventHandler<ChargeCouldNotBeProcessed>,
                                        IEventHandler<ChargeProcessed>, IEventHandler<ChargeSettled>, IEventHandler<ChargeNotSettled>
    {
        protected readonly IQueryableRepository<ChargeProjection> Repository;
        protected readonly IQueryableRepository<ProductProjection> ProductRepository;
        protected readonly IQueryableRepository<ClientApplicationProjection> ClientApplicationRepository;
        protected readonly ICommandScheduler CommandScheduler;
        protected readonly ILogger<ChargeProjection> ChargeProjectionLogger;
        protected readonly IntegrationSettings IntegrationSettings;

        public ChargeEventsHandler(IQueryableRepository<ChargeProjection> repository,
                                    IQueryableRepository<ProductProjection> productRepository,
                                    IQueryableRepository<ClientApplicationProjection> clientApplicationRepository,
                                    ICommandScheduler commandScheduler,
                                    ILogger<ChargeProjection> chargeProjectionLogger, IntegrationSettings integrationSettings)
        {
            Repository = repository;
            ProductRepository = productRepository;
            CommandScheduler = commandScheduler;
            ClientApplicationRepository = clientApplicationRepository;
            ChargeProjectionLogger = chargeProjectionLogger;
            IntegrationSettings = integrationSettings;
        }

        public async Task Handle(ChargeCreated notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);
            var existingProduct = ProductRepository.Get(notification.OrderDetails.ProductInternalKey);
            var existingClientApplication = ClientApplicationRepository.Get(notification.ApplicationKey);

            if (existingProjection == null)
            {
                var projection = new ChargeProjection(notification, existingProduct, existingClientApplication);
                await Repository.AddAynsc(projection);
                await ScheduleChargeProcessing(projection, notification, TimeSpan.Zero);
            }

        }

        public async Task Handle(ChargeProcessed notification, CancellationToken cancellationToken)
        {

            var existingProjection = Repository.Get(notification.AggregateKey);
            if (existingProjection == null)
                throw new AggregateNotFoundException(notification.AggregateKey);

            existingProjection.Update(notification);
            await Repository.UpdateAsync(existingProjection);

        }

        public virtual TimeSpan GetSettlementSchedule(ChargeProjection projection)
        {
            var now = DateTime.UtcNow;
            var chargeDate = projection.OrderDetails?.ChargeDate;
            var interval = IntegrationSettings.SettlementVerificationInterval;
            if (interval < 0) return TimeSpan.Zero;
            var startDate = chargeDate != null && chargeDate >= now ? chargeDate : now;
            return startDate.Value.AddMinutes(interval) - now;
        }

        public async Task Handle(ChargeCouldNotBeProcessed notification, CancellationToken cancellationToken)
        {

            var existingProjection = Repository.Get(notification.AggregateKey);

            if (existingProjection != null)
            {
                existingProjection.Update(notification);
                await Repository.UpdateAsync(existingProjection);

                if (ShouldRetry(existingProjection))
                {
                    int attempt = existingProjection.History.Count(x => x.Status == ChargeStatus.Error) + 1;
                    await ScheduleChargeProcessing(existingProjection, notification, TimeSpan.FromMinutes(IntegrationSettings.ProcessingRetryInterval * attempt));
                }
                else
                {
                    await CommandScheduler.RunNow(new ExpireCharge(notification.AggregateKey,
                                                                   IdentityGenerator.NewSequentialIdentity(),
                                                                   notification.ApplicationKey,
                                                                   notification.SagaProcessKey));
                }
            }
        }

        public async Task Handle(ChargeExpired notification, CancellationToken cancellationToken)
        {

            var existingProjection = Repository.Get(notification.AggregateKey);

            if (existingProjection != null)
            {
                existingProjection.Update(notification);

                await Repository.UpdateAsync(existingProjection);
            }
            else
            {
                throw new AggregateNotFoundException(notification.AggregateKey);
            }
        }

        public async Task Handle(ChargeSettled notification, CancellationToken cancellationToken)
        { 
                var existingProjection = Repository.Get(notification.AggregateKey);

                if (existingProjection == null) throw new AggregateNotFoundException(notification.AggregateKey);

                existingProjection.Update(notification);

                await Repository.UpdateAsync(existingProjection); 
        }

        public async Task Handle(ChargeNotSettled notification, CancellationToken cancellationToken)
        { 
                var existingProjection = Repository.Get(notification.AggregateKey);

                if (existingProjection != null)
                {
                    existingProjection.Update(notification);

                    await Repository.UpdateAsync(existingProjection); 

                    if (ShouldVerifySettlement(existingProjection))
                    {
                        int attempt = existingProjection.History.Count(x => x.Status == ChargeStatus.NotSettled) + 1;
                        await ScheduleSettlementVerification(existingProjection, notification, TimeSpan.FromMinutes(IntegrationSettings.SettlementVerificationInterval * attempt));
                    }
                } 
        }

        public virtual bool ShouldRetry(ChargeProjection existingProjection)
        {
            if (existingProjection.Status == ChargeStatus.Rejected || existingProjection.Status == ChargeStatus.Processed)
                return false;

            var firstErrorHistory = existingProjection
                                        .History
                                            .Where(x => x.Status == ChargeStatus.Error)
                                                .OrderByDescending(x => x.Date).FirstOrDefault();

            if (firstErrorHistory == null) return true;

            return (DateTime.UtcNow - firstErrorHistory.Date).TotalMinutes <= (IntegrationSettings.ProcessingRetryLimit);
        }

        public virtual bool ShouldVerifySettlement(ChargeProjection existingProjection)
        {
            if (existingProjection.Status != ChargeStatus.Processed && existingProjection.Status != ChargeStatus.NotSettled)
                return false;

            var firstVerification = existingProjection
                .History
                .Where(x => x.Status == ChargeStatus.NotSettled)
                .OrderByDescending(x => x.Date).FirstOrDefault();

            if (firstVerification == null) return true;

            return (DateTime.UtcNow - firstVerification.Date).TotalMinutes <= (IntegrationSettings.SettlementVerificationLimit);
        }

        public virtual async Task ScheduleChargeProcessing(ChargeProjection projection, IEvent @event, TimeSpan span)
        {
            if (projection?.Method == PaymentMethod.AcquirerAccount)
            {
                await CommandScheduler.RunDelayed(span, new SendChargeToAcquirer(@event.AggregateKey,
                                                                                 IdentityGenerator.NewSequentialIdentity(),
                                                                                 @event.ApplicationKey,
                                                                                 @event.SagaProcessKey));
            }
        }

        public virtual async Task ScheduleSettlementVerification(ChargeProjection projection, IEvent @event, TimeSpan span)
        {
            if (projection?.Method == PaymentMethod.AcquirerAccount)
            {
                await CommandScheduler.RunDelayed(span, new VerifyAcquirerSettlement(@event.AggregateKey,
                    IdentityGenerator.NewSequentialIdentity(),
                    @event.ApplicationKey,
                    @event.SagaProcessKey));
            }
        }

    }
}
