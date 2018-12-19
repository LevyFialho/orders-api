namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public enum ChargeStatus
    {
        Created = 1,
        Processed = 2,
        Rejected = 3,
        Error = 4,
        Settled = 5,
        NotSettled = 6
    }
}
