using System;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.Model.ChargeAggregate;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Commands
{
    public class ProcessAcquirerReversalCommandTests
    { 
        [Fact]
        public void ValidCommand()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var correlationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var reversalKey = IdentityGenerator.NewSequentialIdentity();

            var command = new ProcessAcquirerAccountReversal(chargeKey, correlationKey, appKey, sagaKey, reversalKey);

            var validationResult = command.IsValid();

            Assert.True(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(reversalKey, command.ReversalKey);
        }
        
        [Fact]
        public void CheckReversalKeyValidation()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var correlationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var reversalKey = string.Empty;

            var command = new ProcessAcquirerAccountReversal(chargeKey, correlationKey, appKey, sagaKey, reversalKey);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(reversalKey, command.ReversalKey);
        }

        [Fact]
        public void CheckChargeKeyValidation()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = string.Empty;
            var correlationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var reversalKey = IdentityGenerator.NewSequentialIdentity();

            var command = new ProcessAcquirerAccountReversal(chargeKey, correlationKey, appKey, sagaKey, reversalKey);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(reversalKey, command.ReversalKey);
        } 
         
    }
}
