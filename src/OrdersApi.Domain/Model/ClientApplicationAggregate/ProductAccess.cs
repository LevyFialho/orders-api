using OrdersApi.Cqrs.Models;

namespace OrdersApi.Domain.Model.ClientApplicationAggregate
{
    public class ProductAccess : ValueObject<ProductAccess>
    {
        public string ProductAggregateKey { get; set; }

        public bool CanCharge { get; set; }

        public bool CanQuery { get; set; }
    }
}
