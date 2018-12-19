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
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.IntegrationServices;

#pragma warning disable S927
#pragma warning disable S1121

namespace OrdersApi.Domain.EventHandlers
{
    public class ReversalEventsHandler : IEventHandler<ReversalCreated>, IEventHandler<AcquirerAccountReversalProcessed>, IEventHandler<AcquirerAccountReversalError>
        , IEventHandler<ReversalSettled>, IEventHandler<ReversalNotSettled>
    {
        protected readonly IQueryableRepository<ChargeProjection> Repository;
        protected readonly ICommandScheduler CommandScheduler;
        protected readonly ILogger<ChargeProjection> ChargeProjectionLogger;
        protected readonly IntegrationSettings IntegrationSettings;

        public ReversalEventsHandler(IQueryableRepository<ChargeProjection> repository,
                                    ICommandScheduler commandScheduler,
                                    ILogger<ChargeProjection> chargeProjectionLogger, IntegrationSettings integrationSettings)
        {
            Repository = repository;
            CommandScheduler = commandScheduler;
            ChargeProjectionLogger = chargeProjectionLogger;
            IntegrationSettings = integrationSettings;
        }

        public async Task Handle(ReversalCreated notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);
            existingProjection.Update(notification);
            await Repository.UpdateAsync(existingProjection);
            await ScheduleReversalProcessing(existingProjection, notification, notification.ReversalKey, TimeSpan.Zero);
        }

        public async Task Handle(AcquirerAccountReversalProcessed notification, CancellationToken cancellationToken)
        {

            var existingProjection = Repository.Get(notification.AggregateKey);
            if (existingProjection == null)
                throw new AggregateNotFoundException(notification.AggregateKey);

            existingProjection.Update(notification);
            await Repository.UpdateAsync(existingProjection);
        }

        public async Task Handle(AcquirerAccountReversalError notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);

            if (existingProjection != null)
            {
                existingProjection.Update(notification);

                await Repository.UpdateAsync(existingProjection);

                if (ShouldRetry(existingProjection, notification.ReversalKey))
                {
                    int attempt = existingProjection.History.Count(x => x.Status == ChargeStatus.Error) + 1;
                    await ScheduleReversalProcessing(existingProjection, notification, notification.ReversalKey, TimeSpan.FromMinutes(IntegrationSettings.ProcessingRetryInterval * attempt));
                }
            }
        }

        public async Task Handle(ReversalSettled notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);
            if (existingProjection == null)
                throw new AggregateNotFoundException(notification.AggregateKey);

            existingProjection.Update(notification);
            await Repository.UpdateAsync(existingProjection);
        }

        public async Task Handle(ReversalNotSettled notification, CancellationToken cancellationToken)
        {
            var existingProjection = Repository.Get(notification.AggregateKey);

            if (existingProjection != null)
            {
                existingProjection.Update(notification);

                await Repository.UpdateAsync(existingProjection);

                if (ShouldVerifySettlement(existingProjection, notification.ReversalKey))
                {
                    int attempt = existingProjection.History.Count(x => x.Status == ChargeStatus.NotSettled) + 1;
                    await ScheduleSettlementVerification(existingProjection, notification, notification.ReversalKey,
                        TimeSpan.FromMinutes(IntegrationSettings.SettlementVerificationInterval * attempt));
                }
            }
        }

        public virtual bool ShouldRetry(ChargeProjection charge, string reversalKey)
        {
            var reversal = charge.Reversals.FirstOrDefault(x => x.ReversalKey == reversalKey);

            if (reversal == null || reversal.Status == ChargeStatus.Processed || reversal.Status == ChargeStatus.Settled)
                return false;

            var firstErrorHistory = reversal
                .History
                .Where(x => x.Status == ChargeStatus.Error)
                .OrderByDescending(x => x.Date).FirstOrDefault();

            if (firstErrorHistory == null) return true;

            return (DateTime.UtcNow - firstErrorHistory.Date).TotalMinutes <= (IntegrationSettings.ProcessingRetryLimit);
        }

        public virtual async Task ScheduleReversalProcessing(ChargeProjection projection, IEvent @event, string reversalKey, TimeSpan span)
        {
            if (projection?.Method == PaymentMethod.AcquirerAccount)
            {
                await CommandScheduler.RunDelayed(span, new ProcessAcquirerAccountReversal(@event.AggregateKey,
                                                                                 IdentityGenerator.NewSequentialIdentity(),
                                                                                 @event.ApplicationKey,
                                                                                 @event.SagaProcessKey, reversalKey));
            }
        }

        public virtual bool ShouldVerifySettlement(ChargeProjection existingProjection, string reversalKey)
        {
            var reversal = existingProjection.Reversals.FirstOrDefault(x => x.ReversalKey == reversalKey);

            if (reversal == null)
                return false;

            if (reversal.Status != ChargeStatus.Processed && reversal.Status != ChargeStatus.NotSettled)
                return false;

            var firstVerification = reversal
                .History
                .Where(x => x.Status == ChargeStatus.NotSettled)
                .OrderByDescending(x => x.Date).FirstOrDefault();

            if (firstVerification == null) return true;

            return (DateTime.UtcNow - firstVerification.Date).TotalMinutes <= (IntegrationSettings.SettlementVerificationLimit);
        }

        public virtual async Task ScheduleSettlementVerification(ChargeProjection projection, IEvent @event, string reversalKey, TimeSpan span)
        {
            if (projection?.Method == PaymentMethod.AcquirerAccount)
            {
                await CommandScheduler.RunDelayed(span, new VerifyReversalSettlement(@event.AggregateKey,
                    IdentityGenerator.NewSequentialIdentity(),
                    @event.ApplicationKey,
                    @event.SagaProcessKey, reversalKey));
            }
        }

    }
}
