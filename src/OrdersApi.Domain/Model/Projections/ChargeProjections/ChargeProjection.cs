using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.Model.ChargeAggregate; 

namespace OrdersApi.Domain.Model.Projections.ChargeProjections
{
    public class ChargeProjection : Projection
    {
        public OrderDetails OrderDetails { get; set; }

        public PaymentMethod Method { get; set; }

        public string CorrelationKey { get; set; }

        public AcquirerAccountInfo AcquirerAccount { get; set; }

        public decimal AmountReverted { get; set; }

        public string ApplicationKey { get; set; }

        public ProductInfo Product { get; set; }

        public ClientApplicationInfo ClientApplication { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? SettlementDate { get; set; }

        public DateTime? LastProcessingDate { get; set; }

        public DateTime? LastSettlementVerificationDate { get; set; }

        public ChargeStatus Status { get; set; }

        public int Version { get; set; }

        public List<ChargeStatusProjection> History { get; set; }

        public List<ReversalProjection> Reversals { get; set; }

        public static Expression<Func<ChargeProjection, bool>> MustScheduleSettlementVerification()
        {
            return (c => c.LastSettlementVerificationDate == null && c.Status == ChargeStatus.Processed);
        }

        public static Expression<Func<ChargeProjection, bool>> MustScheduleReversalsSettlementVerification()
        {
            return (c => c.Reversals != null && c.Reversals.Any(r => r.LastSettlementVerificationDate == null && r.Status == ChargeStatus.Processed));
        }
        
        public ChargeProjection()
        {
            History = new List<ChargeStatusProjection>();
            Reversals = new List<ReversalProjection>();
        }

        public ChargeProjection(ChargeCreated @event, ProductProjection product, ClientApplicationProjection clientApplication)
        {
            var paymentMethod = @event.PaymentMethodData.GetData();
            CreatedDate = @event.EventCommittedTimestamp;
            AggregateKey = @event.AggregateKey;
            Version = @event.TargetVersion + 1;
            OrderDetails = @event.OrderDetails;
            CorrelationKey = @event.CorrelationKey;
            ApplicationKey = @event.ApplicationKey;
            Status = ChargeStatus.Created;
            AcquirerAccount = AcquirerAccountInfo.GetAcquirerAccountInfo(paymentMethod);
            Method = paymentMethod.Method;
            Product = ProductInfo.GetProductInfo(product);
            ClientApplication = ClientApplicationInfo.GetInfo(clientApplication);
            Id = IdentityGenerator.NewSequentialIdentity();
            History = new List<ChargeStatusProjection>();
            Reversals = new List<ReversalProjection>();
            History.Add(new ChargeStatusProjection()
            {
                Status = ChargeStatus.Created,
                Date = @event.EventCommittedTimestamp
            });
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ChargeProcessed @event)
        {
            Status = @event.Result.Success ? ChargeStatus.Processed : ChargeStatus.Rejected;
            LastProcessingDate = @event.EventCommittedTimestamp;
            History.Add(new ChargeStatusProjection()
            {
                Status =  Status,
                Message = string.Join(Environment.NewLine, @event.Result.Details),
                Date = @event.EventCommittedTimestamp,
                IntegrationStatusCode = @event.Result.StatusCode != null ? (int)@event.Result.StatusCode : default(int?)
            });
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ChargeCouldNotBeProcessed @event)
        { 
            LastProcessingDate = @event.EventCommittedTimestamp;
            History.Add(new ChargeStatusProjection()
            {
                Status = ChargeStatus.Error,
                Message = @event.Message,
                Date = @event.EventCommittedTimestamp,
                IntegrationStatusCode = @event.StatusCode
            });
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ChargeSettled @event)
        {
            Status = ChargeStatus.Settled;
            LastSettlementVerificationDate = @event.EventCommittedTimestamp;
            SettlementDate = @event.SettlementDate;
            History.Add(new ChargeStatusProjection()
            {
                Status = Status, 
                Date = @event.EventCommittedTimestamp, 
            });
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ChargeNotSettled @event)
        { 
            LastSettlementVerificationDate = @event.EventCommittedTimestamp;
            History.Add(new ChargeStatusProjection()
            {
                Status = ChargeStatus.NotSettled,
                Message = @event.Result?.Details == null ? String.Empty : string.Join(Environment.NewLine, @event.Result.Details),
                Date = @event.EventCommittedTimestamp,
                IntegrationStatusCode = @event.Result?.StatusCode == null ? 500 : (int)@event.Result.StatusCode
            });
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ReversalCreated @event)
        {
            AmountReverted += @event.Amount;
            Reversals.Add(new ReversalProjection(@event));
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(AcquirerAccountReversalError @event)
        {
          
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
            {
                AmountReverted -= reversal.Amount;
                reversal.Status = ChargeStatus.Error;
                reversal.LastProcessingDate = @event.EventCommittedTimestamp;
                reversal.History.Add(new ChargeStatusProjection()
                {
                    Status = ChargeStatus.Error,
                    Date = @event.EventCommittedTimestamp,
                    Message = @event.Message,
                    IntegrationStatusCode = @event.StatusCode
                });
            }
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(AcquirerAccountReversalProcessed @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
            {
                reversal.Status = @event.Result.Success ? ChargeStatus.Processed : ChargeStatus.Rejected;

                if (reversal.Status == ChargeStatus.Rejected)
                    AmountReverted -= reversal.Amount;

                reversal.LastProcessingDate = @event.EventCommittedTimestamp;
                reversal.History.Add(new ChargeStatusProjection()
                {
                    Status = reversal.Status,
                    Date = @event.EventCommittedTimestamp,
                    Message = string.Join(Environment.NewLine, @event.Result.Details),
                    IntegrationStatusCode = @event.Result.StatusCode != null ? (int)@event.Result.StatusCode : default(int?)
                });
            }
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ReversalSettled @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
            {
                reversal.Status = ChargeStatus.Settled;
                reversal.LastSettlementVerificationDate = @event.EventCommittedTimestamp;
                reversal.SettlementDate = @event.SettlementDate;
                reversal.History.Add(new ChargeStatusProjection()
                {
                    Status = reversal.Status,
                    Date = @event.EventCommittedTimestamp,
                });
            }
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ReversalNotSettled @event)
        {
            var reversal = Reversals.FirstOrDefault(x => x.ReversalKey == @event.ReversalKey);
            if (reversal != null)
            {
                reversal.LastSettlementVerificationDate = @event.EventCommittedTimestamp; 
                reversal.History.Add(new ChargeStatusProjection()
                {
                    Status = ChargeStatus.NotSettled,
                    Date = @event.EventCommittedTimestamp,
                });
            }
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ChargeExpired @event)
        {
            Status = ChargeStatus.Error;
            History.Add(new ChargeStatusProjection()
            {
                Status = Status,
                Date = @event.EventCommittedTimestamp
            });
            Version = @event.TargetVersion + 1;
        }
         
    }
}
