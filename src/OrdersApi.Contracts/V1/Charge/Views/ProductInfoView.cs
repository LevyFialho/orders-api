using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.Contracts.V1.Charge.Views
{
    [ExcludeFromCodeCoverage]
    public class ProductInfoView
    {
        public string Key { get; set; } 

        public string Name { get; set; }
    }
}
