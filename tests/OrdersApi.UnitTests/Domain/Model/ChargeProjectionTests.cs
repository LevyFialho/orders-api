using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ChargeProjectionTests
    {
        [Fact]
        public void CreateProjectionFromEventWithAcquirerAccountPaymentmethodTest()
        { 
            var fixture = new Fixture();
            var @event = fixture.Create<ChargeCreated>();
            var paymentMethod = fixture.Create<AcquirerAccount>();
            @event.PaymentMethodData = new PaymentMethodData(paymentMethod);
            var product = fixture.Create<ProductProjection>();
            var clientApp = fixture.Create<ClientApplicationProjection>();

            var projection = new ChargeProjection(@event, product, clientApp);

            Assert.Equal(@event.CorrelationKey, projection.CorrelationKey);
            Assert.Equal(@event.ApplicationKey, projection.ApplicationKey);
            Assert.Equal(product.Name, projection.Product.Name);
            Assert.Equal(product.ExternalKey, projection.Product.ExternalKey);
            Assert.Equal(clientApp.Name, projection.ClientApplication.Name);
            Assert.Equal(clientApp.ExternalKey, projection.ClientApplication.ExternalKey);
            Assert.Equal(product.AggregateKey, projection.Product.AggregateKey);
            Assert.Equal(@event.AggregateKey, projection.AggregateKey);
            Assert.Equal(@event.EventCommittedTimestamp, projection.CreatedDate);
            Assert.Single(projection.History);
            Assert.Equal(paymentMethod.AcquirerKey, projection.AcquirerAccount.AcquirerKey);
            Assert.Equal(paymentMethod.MerchantKey, projection.AcquirerAccount.MerchantKey); 
            Assert.Equal(paymentMethod.Method, projection.Method);
            Assert.Equal(ChargeStatus.Created, projection.Status);
            Assert.Equal(@event.TargetVersion + 1, projection.Version);
            Assert.False(string.IsNullOrWhiteSpace(projection.Id));
        }

        [Fact]
        public void NullClientApplicationReturnsNullInfo()
        {
            var projection = ClientApplicationInfo.GetInfo(null);
            Assert.Null(projection);

        }

        [Fact]
        public void NullProductReturnsNullInfo()
        {
            var projection = ProductInfo.GetProductInfo(null);
            Assert.Null(projection);
        }

        [Fact]
        public void NullAcquirerAccountInfoReturnsNullInfo()
        {
            var projection = AcquirerAccountInfo.GetAcquirerAccountInfo(null);
            Assert.Null(projection);
        }

        [Fact]
        public void UpdateProjectionFromChargeAProcessedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ChargeProcessed>();
            var projection = fixture.Create<ChargeProjection>();
            int expectedHistoryCount = projection.History.Count + 1;
            int expectedVersion = @event.TargetVersion + 1;
            var status = @event.Result.Success ? ChargeStatus.Processed : ChargeStatus.Rejected;
            projection.Update(@event);   

            Assert.Equal(status, projection.Status);
            Assert.Equal(@event.EventCommittedTimestamp, projection.LastProcessingDate);
            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedHistoryCount, projection.History.Count);
            Assert.Contains(projection.History, x => x.Status == status);
        }

        [Fact]
        public void UpdateProjectionFromChargeCouldNotBeProcessedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ChargeCouldNotBeProcessed>();
            var projection = fixture.Create<ChargeProjection>();
            int expectedHistoryCount = projection.History.Count + 1;
            int expectedVersion = @event.TargetVersion + 1; 
            projection.Update(@event);
            
            Assert.Equal(@event.EventCommittedTimestamp, projection.LastProcessingDate);
            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedHistoryCount, projection.History.Count);
            Assert.Contains(projection.History, x => x.Status == ChargeStatus.Error);
        }

        [Fact]
        public void UpdateProjectionFromChargeExpiredEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ChargeExpired>();
            var projection = fixture.Create<ChargeProjection>();
            int expectedHistoryCount = projection.History.Count + 1;
            int expectedVersion = @event.TargetVersion + 1;
            projection.Update(@event);

            Assert.Equal(ChargeStatus.Error, projection.Status);
            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedHistoryCount, projection.History.Count);
        }

        [Fact]
        public void UpdateProjectionFromChargeNotSettledEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ChargeNotSettled>();
            var projection = fixture.Create<ChargeProjection>();
            int expectedHistoryCount = projection.History.Count + 1;
            int expectedVersion = @event.TargetVersion + 1;
            projection.Update(@event);
            
            Assert.Equal(@event.EventCommittedTimestamp, projection.LastSettlementVerificationDate);
            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedHistoryCount, projection.History.Count);
            Assert.Contains(projection.History, x => x.Status == ChargeStatus.NotSettled);
        }

        [Fact]
        public void UpdateProjectionFromChargeSettledEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ChargeSettled>();
            var projection = fixture.Create<ChargeProjection>();
            int expectedHistoryCount = projection.History.Count + 1;
            int expectedVersion = @event.TargetVersion + 1;
            projection.Update(@event);

            Assert.Equal(ChargeStatus.Settled, projection.Status);
            Assert.Equal(@event.EventCommittedTimestamp, projection.LastSettlementVerificationDate);
            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedHistoryCount, projection.History.Count);
            Assert.Equal(@event.SettlementDate, projection.SettlementDate);
        }

        [Fact]
        public void UpdateProjectionFromReversalCreatedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ReversalCreated>();
            var projection = fixture.Create<ChargeProjection>();
            var expectedAmountReverted = projection.AmountReverted + @event.Amount;
            var expectedReversalsCount = projection.History.Count + 1;
            var expectedVersion = @event.TargetVersion + 1;

            projection.Update(@event);
              
            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedAmountReverted, projection.AmountReverted);
            Assert.Equal(expectedReversalsCount, projection.Reversals.Count); 
        }

        [Fact]
        public void UpdateProjectionFromAcquirerAccountReversalErrorEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<AcquirerAccountReversalError>();
            var projection = fixture.Create<ChargeProjection>();
            var reversal = new ReversalProjection()
            {
                ReversalKey = @event.ReversalKey,
                Status = ChargeStatus.Created,
                Amount = 50m
            };
            projection.Reversals = new List<ReversalProjection>(){reversal};  
            var expectedVersion = @event.TargetVersion + 1;
            var expectedAmountReverted = projection.AmountReverted - reversal.Amount;

            projection.Update(@event);

            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(expectedAmountReverted, projection.AmountReverted);
            Assert.Equal(ChargeStatus.Error, reversal.Status);
            Assert.Equal(1, reversal.History.Count(x => x.Status == ChargeStatus.Error && x.Date == @event.EventCommittedTimestamp && x.Message == @event.Message));
            Assert.Equal(@event.EventCommittedTimestamp, reversal.LastProcessingDate);
        }

        [Fact]
        public void UpdateProjectionFromAcquirerAccountReversalProcessedEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<AcquirerAccountReversalProcessed>();
            @event.Result = new IntegrationResult(Result.Sucess);
            var projection = fixture.Create<ChargeProjection>();
            var reversal = new ReversalProjection()
            {
                ReversalKey = @event.ReversalKey,
                Status = ChargeStatus.Created
            };
            projection.Reversals = new List<ReversalProjection>() { reversal };
            var expectedVersion = @event.TargetVersion + 1;

            projection.Update(@event);

            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(ChargeStatus.Processed, reversal.Status);
            Assert.Equal(1, reversal.History.Count(x => x.Status == ChargeStatus.Processed && x.Date == @event.EventCommittedTimestamp));
            Assert.Equal(@event.EventCommittedTimestamp, reversal.LastProcessingDate);
        }

        [Fact]
        public void UpdateProjectionFromAcquirerAccountReversalProcessedWithErrorEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<AcquirerAccountReversalProcessed>();
            @event.Result = new IntegrationResult(Result.Error);
            var projection = fixture.Create<ChargeProjection>();
            var reversal = new ReversalProjection()
            {
                ReversalKey = @event.ReversalKey,
                Status = ChargeStatus.Created
            };
            projection.Reversals = new List<ReversalProjection>() { reversal };
            var expectedVersion = @event.TargetVersion + 1;
            var expectedAmountReverted = projection.AmountReverted - reversal.Amount;

            projection.Update(@event);

            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(ChargeStatus.Rejected, reversal.Status);
            Assert.Equal(expectedAmountReverted, projection.AmountReverted);
            Assert.Equal(1, reversal.History.Count(x => x.Status == ChargeStatus.Rejected && x.Date == @event.EventCommittedTimestamp));
            Assert.Equal(@event.EventCommittedTimestamp, reversal.LastProcessingDate);
        }

        [Fact]
        public void UpdateProjectionFromReversalSettledEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ReversalSettled>(); 
            var projection = fixture.Create<ChargeProjection>();
            var reversal = new ReversalProjection()
            {
                ReversalKey = @event.ReversalKey,
                Status = ChargeStatus.Created
            };
            projection.Reversals = new List<ReversalProjection>() { reversal };
            var expectedVersion = @event.TargetVersion + 1;

            projection.Update(@event);

            Assert.Equal(expectedVersion, projection.Version);
            Assert.Equal(ChargeStatus.Settled, reversal.Status);
            Assert.Equal(1, reversal.History.Count(x => x.Status == ChargeStatus.Settled && x.Date == @event.EventCommittedTimestamp));
            Assert.Equal(@event.EventCommittedTimestamp, reversal.LastSettlementVerificationDate);
        }

        [Fact]
        public void UpdateProjectionFromReversalNotSettledEventTest()
        {
            var fixture = new Fixture();
            var @event = fixture.Create<ReversalNotSettled>();
            var projection = fixture.Create<ChargeProjection>();
            var reversal = new ReversalProjection()
            {
                ReversalKey = @event.ReversalKey,
                Status = ChargeStatus.Created
            };
            projection.Reversals = new List<ReversalProjection>() { reversal };
            var expectedVersion = @event.TargetVersion + 1;

            projection.Update(@event);

            Assert.Equal(expectedVersion, projection.Version); 
            Assert.Equal(1, reversal.History.Count(x => x.Status == ChargeStatus.NotSettled && x.Date == @event.EventCommittedTimestamp));
            Assert.Equal(@event.EventCommittedTimestamp, reversal.LastSettlementVerificationDate);
        }

    }
}
