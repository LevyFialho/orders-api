using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications; 

namespace OrdersApi.Contracts
{
    [ExcludeFromCodeCoverage]
    public abstract class GetRequest<T> : ClientBoundRequest, IQuery<T> where T : class
    {
        [Required(AllowEmptyStrings = false)] 
        public string InternalKey { get; set; }
         
        public abstract ISpecification<T> Specification();
    }
}
