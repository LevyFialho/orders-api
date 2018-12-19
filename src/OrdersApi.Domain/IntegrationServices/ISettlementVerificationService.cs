using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrdersApi.Domain.IntegrationServices
{
    public interface ISettlementVerificationService
    {
        Task Execute();
    }
}
