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
    public class CreateClientApplicationTests
    {
        [Fact]
        public void GetCommandTest()
        {
            var contract = new Fixture().Create<CreateClientApplication>();
            var command = contract.GetCommand();
            Assert.NotNull(command);
            Assert.Equal(contract.ExternalId, command.ExternalId);
            Assert.Equal(contract.Name, command.Name);
            Assert.Equal(contract.CorrelationKey.ToString("N"), command.CorrelationKey);
            Assert.Equal(IdentityGenerator.DefaultApplicationKey(), command.ApplicationKey);
        }
    }
}
