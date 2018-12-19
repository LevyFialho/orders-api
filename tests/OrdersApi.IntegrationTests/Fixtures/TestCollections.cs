using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
[assembly: TestCollectionOrderer("OrdersApi.ApplicationTests.CollectionOrderer", " OrdersApi.ApplicationTests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace OrdersApi.ApplicationTests.Fixtures
{
    [CollectionDefinition("0")]
    public class HealthcheckCollection : ICollectionFixture<TestContext>
    {

    }

    [CollectionDefinition("1")]
    public class ProductsControllerCollection : ICollectionFixture<TestContext>
    {

    }

    [CollectionDefinition("2")]
    public class ClientApplicationsControllerCollection : ICollectionFixture<TestContext>
    {

    }

    [CollectionDefinition("3")]
    public class ChargesControllerCollection : ICollectionFixture<TestContext>
    {

    }


}
