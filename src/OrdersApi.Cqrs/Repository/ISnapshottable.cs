namespace OrdersApi.Cqrs.Repository
{
    public interface ISnapshottable
    {
        Snapshot TakeSnapshot();
        void ApplySnapshot(Snapshot snapshot);
    }
}
