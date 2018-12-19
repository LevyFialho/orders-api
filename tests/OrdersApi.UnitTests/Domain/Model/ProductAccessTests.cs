using System.Collections.Generic;
using AutoFixture;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ProductAccessTests
    {
        [Fact]
        public void UpdateAccessTest()
        {
            var access = new ProductAccess()
            {
                CanCharge = true,
                CanQuery = true,
                ProductAggregateKey = "X"
            };
            var entity = new Fixture().Create<List<ProductAccess>>();

            entity.UpdateAccess(access);

            Assert.Contains(access, entity);

        }
        [Fact]
        public void RemoveAccessTest()
        {
            var access = new ProductAccess()
            {
                CanCharge = false,
                CanQuery = false,
                ProductAggregateKey = "X"
            };
            var entity = new Fixture().Create<List<ProductAccess>>();

            entity.UpdateAccess(access);

            Assert.DoesNotContain(access, entity);

        }
        /*
         *  public static void UpdateAccess(this List<ProductAccess> list, ProductAccess access)
        {
            var existingAccess = list.FirstOrDefault(x => x.ProductAggregateKey == access.ProductAggregateKey);
            if (existingAccess != null)
                list.Remove(existingAccess);

            if (access.CanCharge || access.CanQuery)
            {
                list.Add(access);
            }
        }
         */
    }
}
