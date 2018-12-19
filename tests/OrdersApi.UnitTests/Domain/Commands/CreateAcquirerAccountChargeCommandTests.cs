using System;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Model.ChargeAggregate;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Commands
{
    public class CreateAcquirerAccountChargeCommandTests
    { 
        [Fact]
        public void ValidCreateAcquirerAccountChargeCommand()
        { 
            var orderDetails = new OrderDetails
            {
                ChargeDate = DateTime.UtcNow.AddHours(25),
                Amount = 10,
                ProductInternalKey = "X"
            };
            var acquirerAccount = new AcquirerAccount
            { 
                MerchantKey = "Y",
                AcquirerKey = "Z"
            };

            var command = new CreateAcquirerAccountCharge(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), orderDetails, acquirerAccount);

            var validationResult = command.IsValid();

            Assert.True(validationResult);
        }

        [Fact]
        public void CheckChargeDateValidation()
        { 
            var orderDetails = new OrderDetails
            {
                ChargeDate = DateTime.UtcNow.Date.AddHours(23).AddMinutes(59),
                Amount = 10,
                ProductInternalKey = "X"
            };
            var acquirerAccount = new AcquirerAccount
            { 
                MerchantKey = "Y",
                AcquirerKey = "Z"
            };
            var command = new CreateAcquirerAccountCharge(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), orderDetails, acquirerAccount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);
        }

        [Fact]
        public void CheckAmountValidation()
        {
            var orderDetails = new OrderDetails
            {
                ChargeDate = DateTime.UtcNow.AddHours(25),
                Amount = 0,
                ProductInternalKey = "X"
            };
            var acquirerAccount = new AcquirerAccount
            { 
                MerchantKey = "Y",
                AcquirerKey = "Z"
            };

            var command = new CreateAcquirerAccountCharge(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), orderDetails, acquirerAccount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);
        }

        [Fact]
        public void CheckProductInternalKeyValidation()
        {
            var orderDetails = new OrderDetails
            {
                ChargeDate = DateTime.UtcNow.AddHours(25),
                Amount = 10,
                ProductInternalKey = string.Empty
            };
            var acquirerAccount = new AcquirerAccount
            { 
                MerchantKey = "Y",
                AcquirerKey = "Z"
            };

            var command = new CreateAcquirerAccountCharge(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), orderDetails, acquirerAccount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(1, command.ValidationResult.Errors.Count);
        }

        [Fact]
        public void CheckAcquirerAccountValidation()
        {
            var orderDetails = new OrderDetails
            {
                ChargeDate = DateTime.UtcNow.AddHours(25),
                Amount = 10,
                ProductInternalKey = "X"
            };
            var acquirerAccount = new AcquirerAccount
            { 
                MerchantKey = string.Empty,
                AcquirerKey = string.Empty
            };

            var command = new CreateAcquirerAccountCharge(IdentityGenerator.NewSequentialIdentity(),
                IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), IdentityGenerator.NewSequentialIdentity(), orderDetails, acquirerAccount);

            var validationResult = command.IsValid();

            Assert.False(validationResult);
            Assert.Equal(2, command.ValidationResult.Errors.Count);
        }
         
    }
}
