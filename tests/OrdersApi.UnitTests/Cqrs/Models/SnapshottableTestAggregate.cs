using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;

namespace OrdersApi.UnitTests.Cqrs.Models
{
    public class SnapshottableTestAggregate : AggregateRoot, ISnapshottable
    {
        [InternalEventHandler]
        public void OnTestEvent(TestEvent @event)
        { 
        }
        public SnapshottableTestAggregate(): base()
        {
            
        }

        public SnapshottableTestAggregate(TestEvent e): base()
        {
           ApplyEvent(e);
        }
        
        public virtual Snapshot TakeSnapshot()
        {
            return new Snapshot(IdentityGenerator.NewSequentialIdentity(), AggregateKey, CurrentVersion);
        }

        public void ApplySnapshot(Snapshot snapshot)
        {
            AggregateKey = snapshot.AggregateKey;
            CurrentVersion = snapshot.Version;
        }
    }

   
}
