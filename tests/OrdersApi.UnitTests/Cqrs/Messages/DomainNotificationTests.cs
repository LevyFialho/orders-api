using OrdersApi.Cqrs.Messages;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Messages
{
    public class DomainNotificationTests
    {
        [Fact]
        public void ConstructorTest()
        {
            string key = "XOX";
            string value = "XOOX";
            var notification = new DomainNotification(key, value);
            Assert.Equal(key, notification.Key);
            Assert.Equal(value, notification.Value);
            Assert.Equal(1, notification.Version);
            Assert.NotEqual(string.Empty, notification.DomainNotificationKey);
        }
    }
}
