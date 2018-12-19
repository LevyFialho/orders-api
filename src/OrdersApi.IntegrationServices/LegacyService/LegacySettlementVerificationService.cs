using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Domain.Specifications;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Domain.Commands.Charge.Reversal;
using Hangfire;

namespace OrdersApi.IntegrationServices.LegacyService
{

    public class LegacySettlementVerificationService : ISettlementVerificationService
    {
        private readonly ICommandBus _commandBus;
        private readonly IQueryableRepository<ChargeProjection> _repository;
        private readonly AcquirerSettlementVerificationSettings _settings;
        private readonly ILogger<LegacySettlementVerificationService> _logger;
        private bool _hasChargesToVerify = true;
        private bool _hasReversalsToVerify = true;

        public LegacySettlementVerificationService(ICommandBus commandBus, IQueryableRepository<ChargeProjection> repository, IOptions<AcquirerSettlementVerificationSettings> settings, ILogger<LegacySettlementVerificationService> logger)
        {
            this._commandBus = commandBus;
            this._repository = repository;
            this._settings = settings.Value;
            this._logger = logger;

            this.ChargeDate = DateTime.UtcNow.Date;

            this.SettlementVerificationDate = this.ChargeDate.AddDays(1);
        }

        public virtual DateTime ChargeDate { get; set; }

        public virtual DateTime SettlementVerificationDate { get; set; }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 30 * 60)]
        public virtual async Task Execute()
        {
            try
            {
                while (_hasChargesToVerify)
                {
                    await VerifyCharges();
                }

                while (_hasReversalsToVerify)
                {
                    await VerifyReversals();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }

        }

        public virtual async Task VerifyCharges()
        {
            var specifications = ChargeSpecifications.ProjectionByChargeDate(from: null, to: this.ChargeDate) && ChargeSpecifications.MustScheduleSettlementVerification();

            var charges = await _repository.GetFilteredSortByAsync(specifications, x => x.OrderDetails.ChargeDate, this._settings.ChargesPerExecution);

            if (charges == null || !charges.Any())
            {
                _hasChargesToVerify = false;
                return;
            }
            foreach (var charge in charges)
            {
                await SendVerifySettlementCommandForCharge(charge);
            }

        }

        public virtual async Task SendVerifySettlementCommandForCharge(ChargeProjection charge)
        {
            var verifySettlementCommand = new VerifyAcquirerSettlement(aggregateKey: charge.AggregateKey,
                                                                        correlationKey: IdentityGenerator.NewSequentialIdentity(),
                                                                        applicationKey: charge.ApplicationKey,
                                                                        sagaProcessKey: IdentityGenerator.NewSequentialIdentity());

            await _commandBus.Publish(verifySettlementCommand, SettlementVerificationDate);

            charge.LastSettlementVerificationDate = SettlementVerificationDate;

            _repository.Update(charge);

            this._logger.LogInformation("Charge \"@charge\" scheduled for settlement verification at @settlementDate", charge, this.ChargeDate);
        }

        public virtual async Task VerifyReversals()
        {
            var reversalSpecifications =
                                ChargeSpecifications.ProjectionByReversalDate(from: null, to: this.ChargeDate) &&
                                ChargeSpecifications.HasReversalsToVerifySettlement();

            var reversedCharges = await _repository.GetFilteredSortByAsync(reversalSpecifications, x => x.OrderDetails.ChargeDate, this._settings.ChargesPerExecution);
            if (reversedCharges == null || !reversedCharges.Any())
            {
                _hasReversalsToVerify = false;
                return;
            }
            foreach (var reversedCharge in reversedCharges)
            {
                var reversals = reversedCharge.Reversals.Where(ReversalProjection.MustScheduleSettlementVerification());

                foreach (var reversal in reversals)
                {
                    await SendVerifySettlementCommandForReversal(reversedCharge, reversal);
                }
            }
        }

        public virtual async Task SendVerifySettlementCommandForReversal(ChargeProjection charge, ReversalProjection reversal)
        {
            var verifySettlementCommand = new VerifyReversalSettlement(aggregateKey: charge.AggregateKey,
                                                                        correlationKey: IdentityGenerator.NewSequentialIdentity(),
                                                                        applicationKey: charge.ApplicationKey,
                                                                        sagaProcessKey: IdentityGenerator.NewSequentialIdentity(),
                                                                        reversalKey: reversal.ReversalKey);

            await _commandBus.Publish(verifySettlementCommand, SettlementVerificationDate);

            reversal.LastSettlementVerificationDate = SettlementVerificationDate;

            _repository.Update(charge);

            this._logger.LogInformation("Reversal \"@reversal\" scheduled for settlement verification at @settlementDate", reversal, this.ChargeDate);
        }
    }
}
