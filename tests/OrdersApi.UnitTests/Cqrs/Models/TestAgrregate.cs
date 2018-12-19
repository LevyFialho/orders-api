using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.UnitTests.Cqrs.Models
{
    public class TestAggregate : AggregateRoot
    {
        [InternalEventHandler]
        public void OnTestEvent(TestEvent @event)
        {
            AggregateKey = @event.AggregateKey;
        }
        public TestAggregate(): base()
        {
            
        }

        public TestAggregate(TestEvent e): base()
        {
           ApplyEvent(e);
        }

        public void SetCurrentVersion(short version)
        {
            CurrentVersion = version;
        }
    }

   
}
