using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.Snapshots;
#pragma warning disable S3626

namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public class Charge : AggregateRoot
    {
        public virtual OrderDetails OrderDetails { get; set; }

        public virtual string CorrelationKey { get; set; }

        public virtual PaymentMethodData PaymentMethodData { get; set; }

        public virtual DateTime CreatedDate { get; set; }

        public virtual DateTime? SettlementDate { get; set; }

        public virtual ChargeStatus Status { get; set; }

        public virtual List<Reversal> Reversals { get; set; }

        public Charge()
        {
            Reversals = new List<Reversal>();
        }

        public Charge(string aggregateKey, string correlationKey, string applicationKey,
            string sagaProcessKey, OrderDetails orderDetails, PaymentMethodData paymentMethodData) : this()
        {
            ApplyEvent(new ChargeCreated(aggregateKey, correlationKey, applicationKey, sagaProcessKey, orderDetails, paymentMethodData));
            Reversals = new List<Reversal>();
        }

        public virtual bool CanRevert(decimal amount = 0)
        {
            bool canRevert = Status == ChargeStatus.Processed || Status == ChargeStatus.Settled;
            var totalAmount = amount + Reversals.Where(x =>
                                  x.Status == ChargeStatus.Created || x.Status == ChargeStatus.Processed ||
                                  x.Status == ChargeStatus.Settled).Sum(x => x.Amount);
            return canRevert && totalAmount <= OrderDetails.Amount;
        }

        public virtual async void SendToAcquirer(string correlationKey, string applicationKey,
            string sagaProcessKey, ILegacyApiService integrationService)
        {
            if (Status != ChargeStatus.Error)
            {
                var account = PaymentMethodData?.GetData() as AcquirerAccount;
                var checkIfAlreadySentResult = await integrationService.CheckIfChargeOrderWasSent(account, AggregateKey);
                if (checkIfAlreadySentResult == null || !checkIfAlreadySentResult.Success)
                {
                    ApplyEvent(new ChargeCouldNotBeProcessed(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, "Could not check if order was already sent"));
                    return;
                }
                if (!checkIfAlreadySentResult.ReturnedObject)
                {
                    var sendOrderResult = await integrationService.SendChargeOrder(this);
                    if (sendOrderResult == null || !sendOrderResult.Success)
                    {
                        ApplyEvent(new ChargeCouldNotBeProcessed(AggregateKey, correlationKey, applicationKey,
                            sagaProcessKey, CurrentVersion, "Could not send order"));
                        return;
                    }
                    ApplyEvent(new ChargeProcessed(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, sendOrderResult));
                    return;
                }
                ApplyEvent(new ChargeProcessed(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, checkIfAlreadySentResult));
            }
        }

        public virtual async void SendReversalToAcquirer(string correlationKey, string applicationKey,
            string sagaProcessKey, string reversalKey, ILegacyApiService integrationService)
        {
            var account = PaymentMethodData?.GetData() as AcquirerAccount;
            var checkIfAlreadySentResult = await integrationService.CheckIfChargeOrderWasSent(account, reversalKey);
            if (checkIfAlreadySentResult == null || !checkIfAlreadySentResult.Success)
            {
                ApplyEvent(new AcquirerAccountReversalError(AggregateKey, correlationKey, applicationKey,
                    sagaProcessKey, reversalKey, CurrentVersion, "Could not check if reversal was already sent"));
                return;
            }
            if (!checkIfAlreadySentResult.ReturnedObject)
            {
                var sendOrderResult = await integrationService.SendReversalOrder(this, reversalKey);
                if (sendOrderResult == null || !sendOrderResult.Success)
                {
                    ApplyEvent(new AcquirerAccountReversalError(AggregateKey, correlationKey, applicationKey,
                        sagaProcessKey, reversalKey, CurrentVersion, "Could not send reversal order"));
                    return;
                }
                ApplyEvent(new AcquirerAccountReversalProcessed(AggregateKey, correlationKey, applicationKey,
                    sagaProcessKey, reversalKey, CurrentVersion, sendOrderResult));
                return;
            }
            ApplyEvent(new AcquirerAccountReversalProcessed(AggregateKey, correlationKey, applicationKey,
                sagaProcessKey, reversalKey, CurrentVersion, checkIfAlreadySentResult));

        }

        public virtual async void VerifyReversalSettlement(string correlationKey, string applicationKey,
            string sagaProcessKey, string reversalKey, ILegacyApiService integrationService)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == reversalKey);
            if (reversal != null && (reversal.Status == ChargeStatus.Processed ||
                reversal.Status == ChargeStatus.NotSettled))
            {
                var account = PaymentMethodData?.GetData() as AcquirerAccount;
                var settlementResult = await integrationService.GetSettlementDate(account, reversalKey);
                if (settlementResult == null || !settlementResult.Success || settlementResult.ReturnedObject == null)
                {
                    ApplyEvent(new ReversalNotSettled(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, settlementResult, reversalKey));
                    return;
                }
                ApplyEvent(new ReversalSettled(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, settlementResult.ReturnedObject.Value, reversalKey));
            }

        }

        public virtual async void VerifySettlement(string correlationKey, string applicationKey,
            string sagaProcessKey, ILegacyApiService integrationService)
        {
            if (CanVerifySettlement)
            {
                var account = PaymentMethodData?.GetData() as AcquirerAccount;
                var settlementResult = await integrationService.GetSettlementDate(account, AggregateKey);
                if (settlementResult == null || !settlementResult.Success || settlementResult.ReturnedObject == null)
                {
                    ApplyEvent(new ChargeNotSettled(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, settlementResult));
                    return;
                }
                ApplyEvent(new ChargeSettled(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion, settlementResult.ReturnedObject.Value));
            }
        }

        public bool CanVerifySettlement => Status != ChargeStatus.Error && Status != ChargeStatus.Settled;

        public virtual void Expire(string correlationKey, string applicationKey, string sagaProcessKey)
        {
            ApplyEvent(new ChargeExpired(AggregateKey, correlationKey, applicationKey, sagaProcessKey, CurrentVersion));
        }

        public virtual void Revert(string correlationKey, string applicationKey, string sagaProcesskey, decimal amount, string reversalKey)
        {
            DateTime reversalDate = GetReversalDate();

            ApplyEvent(new ReversalCreated(AggregateKey, correlationKey, applicationKey, sagaProcesskey,
                reversalKey, reversalDate, amount, CurrentVersion));
        }

        public virtual DateTime GetReversalDate()
        {
            DateTime reversalDate = DateTime.UtcNow.Date.AddDays(1);
            if (OrderDetails.ChargeDate.Date > reversalDate)
                reversalDate = OrderDetails.ChargeDate.Date;

            return reversalDate;
        }

        [InternalEventHandler]
        public virtual void OnChargeCreated(ChargeCreated @event)
        {
            OrderDetails = @event.OrderDetails;
            PaymentMethodData = @event.PaymentMethodData;
            AggregateKey = @event.AggregateKey;
            CreatedDate = @event.EventCommittedTimestamp;
            Status = ChargeStatus.Created;
            CorrelationKey = @event.CorrelationKey;
        }

        [InternalEventHandler]
        public virtual void OnChargeNotSettled(ChargeNotSettled @event)
        {
            return;
        }

        [InternalEventHandler]
        public virtual void OnChargeSettled(ChargeSettled @event)
        {
            Status = ChargeStatus.Settled;
            SettlementDate = @event.SettlementDate;
        }

        [InternalEventHandler]
        public virtual void OnChargeProcessed(ChargeProcessed @event)
        {
            Status = @event.Result.Success ? ChargeStatus.Processed : ChargeStatus.Rejected;
        }

        [InternalEventHandler]
        public virtual void OnChargeCouldNotBeProcessed(ChargeCouldNotBeProcessed @event)
        {
            return;
        }


        [InternalEventHandler]
        public virtual void OnAcquirerAccountReversalProcessed(AcquirerAccountReversalProcessed @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
                reversal.Status = @event.Result.Success ? ChargeStatus.Processed : ChargeStatus.Rejected;
        }

        [InternalEventHandler]
        public virtual void OnAcquirerAccountReversalError(AcquirerAccountReversalError @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
                reversal.Status = ChargeStatus.Error;
        }

        [InternalEventHandler]
        public virtual void OnReversalSettled(ReversalSettled @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
                reversal.Status = ChargeStatus.Settled;
        }

        [InternalEventHandler]
        public virtual void OnReversalNotSettled(ReversalNotSettled @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
                reversal.Status = ChargeStatus.Processed;
        }

        [InternalEventHandler]
        public virtual void OnChargeReversalCreated(ReversalCreated @event)
        {
            Reversals.Add(new Reversal()
            {
                Amount = @event.Amount,
                Status = ChargeStatus.Created,
                ReversalDueDate = @event.ReversalDueDate,
                ReversalKey = @event.ReversalKey
            });
        }

        [InternalEventHandler]
        public virtual void OnChargeExpired(ChargeExpired @event)
        {
            Status = ChargeStatus.Error;
        }

        public virtual Snapshot TakeSnapshot()
        {
            return new ChargeSnapshot(IdentityGenerator.NewSequentialIdentity(), this);
        }

        public virtual void ApplySnapshot(Snapshot snapshot)
        {
            if (!(snapshot is ChargeSnapshot appSnapshot)) return;
            PaymentMethodData = appSnapshot.PaymentMethodData;
            OrderDetails = appSnapshot.OrderDetails;
            AggregateKey = appSnapshot.AggregateKey;
            Status = appSnapshot.Status;
            CurrentVersion = appSnapshot.Version;
            Reversals = appSnapshot.Reversals ?? new List<Reversal>();
        }

    }
}
