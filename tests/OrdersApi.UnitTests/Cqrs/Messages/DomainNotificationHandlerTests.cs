using System.Threading;
using OrdersApi.Cqrs.Messages;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Messages
{
    public class DomainNotificationHandlerTests
    {
        [Fact]
        public void HandleTest()
        {
            var message = new DomainNotification("XOX", "XOOX");
            var handler = new DomainNotificationHandler();
            Assert.False(handler.HasNotifications());
            Assert.Empty(handler.GetNotifications());
            handler.Handle(message, CancellationToken.None);
            Assert.True(handler.HasNotifications());
            Assert.Single(handler.GetNotifications());
        }
    }
}
