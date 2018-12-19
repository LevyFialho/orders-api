using System;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.Model.ChargeAggregate;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Commands
{
    public class CreateChargeReversalCommandTests
    { 
        [Fact]
        public void ValidCommand()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var correlationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var amount = 100m;

            var command = new CreateChargeReversal(chargeKey, correlationKey, appKey, sagaKey, amount);

            var validationResult = command.IsValid();

            Assert.True(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(amount, command.Amount);
        }
        
        [Fact]
        public void CheckAmountValidation()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var correlationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var amount = -100m;

            var command = new CreateChargeReversal(chargeKey, correlationKey, appKey, sagaKey, amount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(amount, command.Amount);
        }

        [Fact]
        public void CheckChargeKeyValidation()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = string.Empty;
            var correlationKey = IdentityGenerator.NewSequentialIdentity();
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var amount = 100m;

            var command = new CreateChargeReversal(chargeKey, correlationKey, appKey, sagaKey, amount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(amount, command.Amount);
        }

        [Fact]
        public void CheckCorrelationKeyValidation()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var correlationKey = string.Empty;
            var sagaKey = IdentityGenerator.NewSequentialIdentity();
            var amount = 100m;

            var command = new CreateChargeReversal(chargeKey, correlationKey, appKey, sagaKey, amount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(correlationKey, command.CorrelationKey);
            Assert.Equal(sagaKey, command.SagaProcessKey);
            Assert.Equal(amount, command.Amount);
        }
         
    }
}
