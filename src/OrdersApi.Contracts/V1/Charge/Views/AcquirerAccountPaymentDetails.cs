using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Contracts.V1.Charge.Views
{
    [ExcludeFromCodeCoverage]
    public class AcquirerAccountPaymentDetails
    {
        public string AcquirerKey { get; set; }

        public string MerchantKey { get; set; }

        public int AccountType { get; set; }

    }
}
