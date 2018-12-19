using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Model.ChargeAggregate;

namespace OrdersApi.Domain.IntegrationServices
{
    public interface IAcquirerApiService
    {
        Task<IntegrationResult<bool>> CheckIfChargeOrderWasSent(AcquirerAccount account, string orderId);
        
        Task<IntegrationResult<DateTime?>> GetSettlementDate(AcquirerAccount account, string orderId);

        Task<IntegrationResult> SendChargeOrder(Charge charge);

        Task<IntegrationResult> SendReversalOrder(Charge charge, string reversalKey);
    }
}
