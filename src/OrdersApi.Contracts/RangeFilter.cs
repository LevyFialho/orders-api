
using System.Diagnostics.CodeAnalysis;

namespace OrdersApi.Contracts
{
    [ExcludeFromCodeCoverage]
    public class RangeFilter<T> 
    {
        public T From { get; set; }

        public T To { get; set; }
    }
}
