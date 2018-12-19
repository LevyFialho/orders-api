using AutoFixture;
using OrdersApi.Contracts.V1.Product.Commands;
using OrdersApi.Cqrs.Models;
using Xunit;

namespace OrdersApi.UnitTests.Contracts
{
    public class CreateProductTests
    {
        [Fact]
        public void GetCommandTest()
        { 
            var contract = new Fixture().Create<CreateProduct>(); 
            var command = contract.GetCommand();
            Assert.NotNull(command);
            Assert.Equal(contract.ExternalId, command.ExternalId);
            Assert.Equal(contract.Name, command.Name);
            Assert.Equal(contract.CorrelationKey.ToString("N"), command.CorrelationKey);
            Assert.Equal(IdentityGenerator.DefaultApplicationKey(), command.ApplicationKey);
        }
    }
}
