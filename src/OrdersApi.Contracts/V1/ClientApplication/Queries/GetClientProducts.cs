using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;
using Newtonsoft.Json;

namespace OrdersApi.Contracts.V1.ClientApplication.Queries
{
    [ExcludeFromCodeCoverage]
    public class GetClientProducts 
    { 
        public bool? CanCharge { get; set; }

        public bool? CanQuery { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageSize { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; }
         
    }
}
