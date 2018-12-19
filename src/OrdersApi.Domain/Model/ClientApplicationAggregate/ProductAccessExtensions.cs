using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrdersApi.Domain.Model.ClientApplicationAggregate
{
    public static class ProductAccessExtensions
    {
        public static void UpdateAccess(this List<ProductAccess> list, ProductAccess access)
        {
            var existingAccess = list.FirstOrDefault(x => x.ProductAggregateKey == access.ProductAggregateKey);
            if (existingAccess != null)
                list.Remove(existingAccess);

            if (access.CanCharge || access.CanQuery)
            {
                list.Add(access);
            }
        }
    }
}
