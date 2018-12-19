using System;
using System.Collections.Generic;
using System.Text;
using AutoFixture;
using OrdersApi.Contracts.V1.Charge.Commands;
using OrdersApi.Cqrs.Models;
using Xunit;

namespace OrdersApi.UnitTests.Contracts
{
    public class CreateAcquirerAccountChargeTests
    {
        [Fact]
        public void GetCommandTest()
        {
            var appKey = IdentityGenerator.DefaultApplicationKey();
            var productKey = IdentityGenerator.NewSequentialIdentity();
            var contract = new CreateAcquirerAccountCharge()
            {
                CorrelationKey = Guid.NewGuid(),
                Amount = 100m,
                ChargeDate = DateTime.Today.AddDays(1),
                ProductExternalKey = "X",
                Payment = new AcquirerAccountDetails()
                {
                    MerchantKey = "MerchantKey",
                    AcquirerKey = "AcquirerKey", 
                }
            };
            contract.SetInternalApplicationKey(appKey);
            contract.SetInternalProductKey(productKey);
            var command = contract.GetCommand();
            Assert.NotNull(command);
            Assert.Equal(contract.Amount, command.OrderDetails.Amount);
            Assert.Equal(productKey, command.OrderDetails.ProductInternalKey);
            Assert.Equal(contract.ChargeDate, command.OrderDetails.ChargeDate); 
            Assert.Equal(contract.Payment.AcquirerKey, command.AcquirerAccount.AcquirerKey);
            Assert.Equal(contract.Payment.MerchantKey, command.AcquirerAccount.MerchantKey);
        }
    }
}
