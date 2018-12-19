using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts
{
    [ExcludeFromCodeCoverage]
    public class GetChargeResponse
    {
        public virtual bool IsSettled { get; set; }

        public virtual string AffiliationKey { get; set; }

        public virtual DateTime ChargeDate { get; set; }

        public virtual DateTime EntryDateTime { get; set; }

        public virtual int ChargeType { get; set; }

        public virtual ChargeAmount ChargeAmount { get; set; }

        public virtual Status SettlementStatus { get; set; }
    }
     
}
