using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.ClientApplication;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Commands
{
    public class ClientApplicationCommandsTests
    {
        [Fact]
        public void EmptyExternalId()
        {
            var command = new CreateClientApplication(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(),
                string.Empty, IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity());

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);
         
        }

        [Fact]
        public void EmptyName()
        {
            var command = new CreateClientApplication(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), string.Empty, IdentityGenerator.NewSequentialIdentity());

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);

        }

        [Fact]
        public void ValidCreateClientApplicationCommand()
        {
            var command = new CreateClientApplication(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity());

            var validationResult = command.IsValid();

            Assert.True(validationResult); 
        }
    }
}
