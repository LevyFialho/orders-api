using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.Product;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Commands
{
    public class ProductCommandsTests
    {
        public class SetProductAcquirerConfigurationCommandTests
        {
            [Fact]
            public void SetProductAcquirerConfigurationValidCommand()
            { 
                var acquirerKey = IdentityGenerator.NewSequentialIdentity();
                var accountKey = IdentityGenerator.NewSequentialIdentity(); 

                var command = new UpdateProductAcquirerConfiguration(
                    aggregateKey: IdentityGenerator.NewSequentialIdentity(),
                    correlationKey: IdentityGenerator.NewSequentialIdentity(),
                    applicationKey: IdentityGenerator.NewSequentialIdentity(),
                    sagaProcessKey: IdentityGenerator.NewSequentialIdentity(),
                    acquirerKey: acquirerKey,
                    accountKey: accountKey,
                    isEnabled: true);

                Assert.True(command.IsValid());
                Assert.True(command.Configuration.CanCharge);
                Assert.Equal(acquirerKey, command.Configuration.AcquirerKey);
                Assert.Equal(accountKey, command.Configuration.AccountKey);
            }

            [Fact]
            public void SetProductAcquirerConfigurationInValidIfEmptyAcquirerKey()
            {
                var acquirerKey = string.Empty;
                var accountKey = IdentityGenerator.NewSequentialIdentity();

                var command = new UpdateProductAcquirerConfiguration(
                    aggregateKey: IdentityGenerator.NewSequentialIdentity(),
                    correlationKey: IdentityGenerator.NewSequentialIdentity(),
                    applicationKey: IdentityGenerator.NewSequentialIdentity(),
                    sagaProcessKey: IdentityGenerator.NewSequentialIdentity(),
                    acquirerKey: acquirerKey,
                    accountKey: accountKey,
                    isEnabled: true);

                Assert.False(command.IsValid());
                Assert.True(command.Configuration.CanCharge);
                Assert.Equal(acquirerKey, command.Configuration.AcquirerKey);
                Assert.Equal(accountKey, command.Configuration.AccountKey);
            }

            [Fact]
            public void SetProductAcquirerConfigurationInValidIfEmptyAccountTypeId()
            {
                var acquirerKey = IdentityGenerator.NewSequentialIdentity();
                var accountKey = string.Empty;

                var command = new UpdateProductAcquirerConfiguration(
                    aggregateKey: IdentityGenerator.NewSequentialIdentity(),
                    correlationKey: IdentityGenerator.NewSequentialIdentity(),
                    applicationKey: IdentityGenerator.NewSequentialIdentity(),
                    sagaProcessKey: IdentityGenerator.NewSequentialIdentity(),
                    acquirerKey: acquirerKey,
                    accountKey: accountKey,
                    isEnabled: true);

                Assert.False(command.IsValid());
                Assert.True(command.Configuration.CanCharge);
                Assert.Equal(acquirerKey, command.Configuration.AcquirerKey);
                Assert.Equal(accountKey, command.Configuration.AccountKey);
            }
        }

        [Fact]
        public void EmptyExternalId()
        {
            var command = new CreateProduct(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(),
                string.Empty, IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity()); 

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);
         
        }

        [Fact]
        public void EmptyName()
        {
            var command = new CreateProduct(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), string.Empty, IdentityGenerator.NewSequentialIdentity());

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);

        }

        [Fact]
        public void ValidCreateProductCommand()
        {
            var command = new CreateProduct(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity());

            var validationResult = command.IsValid();

            Assert.True(validationResult); 
        }
    }
}
