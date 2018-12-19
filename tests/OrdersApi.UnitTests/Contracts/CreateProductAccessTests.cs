using System;
using System.Collections.Generic;
using System.Text;
using AutoFixture;
using OrdersApi.Contracts.V1.Charge.Commands;
using OrdersApi.Contracts.V1.ClientApplication.Commands;
using OrdersApi.Cqrs.Models;
using Xunit;

namespace OrdersApi.UnitTests.Contracts
{
    public class CreateProductAccessTests
    {
        [Fact]
        public void GetCommandTest()
        { 
            var contract = new Fixture().Create<CreateProductAccess>(); 
            var command = contract.GetCommand();
            Assert.NotNull(command);
            Assert.Equal(contract.ProductInternalKey, command.ProductAggregateKey);
            Assert.Equal(contract.CanCharge, command.CanCharge);
            Assert.Equal(contract.CanQuery, command.CanQuery);
            Assert.Equal(contract.AggregateKey, command.AggregateKey);
            Assert.Equal(contract.CorrelationKey.ToString("N"), command.CorrelationKey);
            Assert.Equal(IdentityGenerator.DefaultApplicationKey(), command.ApplicationKey);
        }
    }
}
