using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis; 

namespace OrdersApi.Contracts.V1.Charge.Commands
{
    [ExcludeFromCodeCoverage]
    public class AcquirerAccountDetails  
    {
        [Required(AllowEmptyStrings = false)]
        public string AcquirerKey { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string MerchantKey { get; set; } 
    }
}
