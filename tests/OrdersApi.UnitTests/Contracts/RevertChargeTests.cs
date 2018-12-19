using System;
using System.Collections.Generic;
using System.Text;
using AutoFixture;
using OrdersApi.Contracts.V1.Charge.Commands;
using OrdersApi.Cqrs.Models;
using Xunit;

namespace OrdersApi.UnitTests.Contracts
{
    public class RevertChargeTests
    {
        [Fact]
        public void GetCommandTest()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var contract = new RevertCharge()
            {
                CorrelationKey = Guid.NewGuid(),
                Amount = 100m,
                
            };
            contract.SetInternalApplicationKey(appKey);
            contract.SetChargeKey(chargeKey);
            var command = contract.GetCommand();
            Assert.NotNull(command);
            Assert.Equal(contract.Amount, command.Amount);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.False(string.IsNullOrWhiteSpace(command.ReversalKey));
            Assert.True(command.IsValid());
             
        }


        [Fact]
        public void InvalidCommandAmountTest()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var chargeKey = IdentityGenerator.NewSequentialIdentity();
            var contract = new RevertCharge()
            {
                CorrelationKey = Guid.NewGuid(),
                Amount = -100m,

            };
            contract.SetInternalApplicationKey(appKey);
            contract.SetChargeKey(chargeKey);
            var command = contract.GetCommand();
            Assert.NotNull(command);
            Assert.Equal(contract.Amount, command.Amount);
            Assert.Equal(chargeKey, command.AggregateKey);
            Assert.Equal(appKey, command.ApplicationKey);
            Assert.False(string.IsNullOrWhiteSpace(command.ReversalKey));
            Assert.False(command.IsValid());

        }
    }
}
