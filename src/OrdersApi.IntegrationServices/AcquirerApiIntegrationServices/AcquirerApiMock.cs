using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate;

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices
{
    [ExcludeFromCodeCoverage]
    public class AcquirerApiMock: IAcquirerApiService
    {
        public Task<IntegrationResult<bool>> CheckIfChargeOrderWasSent(AcquirerAccount account, string orderId)
        {
            return Task.FromResult(new IntegrationResult<bool>(Result.Sucess) { ReturnedObject = false });
        }

        public Task<IntegrationResult<DateTime?>> GetSettlementDate(AcquirerAccount account, string orderId)
        {
            return Task.FromResult(new IntegrationResult<DateTime?>(Result.Sucess) { ReturnedObject = DateTime.UtcNow.AddMinutes(-1) });
        }

        public Task<IntegrationResult> SendChargeOrder(Charge charge)
        {
            return Task.FromResult(new IntegrationResult(Result.Sucess));
        }

        public Task<IntegrationResult> SendReversalOrder(Charge charge, string reversalKey)
        {
            return Task.FromResult(new IntegrationResult(Result.Sucess));
        }
    }
}
