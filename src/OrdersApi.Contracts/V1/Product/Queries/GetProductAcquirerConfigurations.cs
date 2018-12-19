using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;
using Newtonsoft.Json;

namespace OrdersApi.Contracts.V1.Product.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetProductAcquirerConfigurations
    { 
        public bool? CanCharge { get; set; }
        
        public string AccountKey { get; set; }

        public string Acquirerkey { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageSize { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; }
         
    }
}
