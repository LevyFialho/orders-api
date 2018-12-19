using AutoFixture;
using OrdersApi.Domain.Model.ChargeAggregate;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class PaymentMethodDataTests
    {
        [Fact]
        public void GetAcquirerAccountDataTests()
        {
            var fixture = new Fixture();
            var paymentDetails = fixture.Create<AcquirerAccount>();

            var paymentData = new PaymentMethodData(paymentDetails);
            var recoveredData = paymentData.GetData() as AcquirerAccount;

            Assert.Equal(paymentDetails.GetType().AssemblyQualifiedName, paymentData.DataType);
            Assert.NotNull(recoveredData);
        } 
    }
}
