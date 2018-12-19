using System.Collections.Generic;
using System.Linq; 
using Xunit;
using Xunit.Abstractions; 

namespace OrdersApi.ApplicationTests
{
    public class CollectionOrderer : ITestCollectionOrderer
    {
        public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        {
            return testCollections.OrderBy(collection => collection.DisplayName);
        }
    }
}
