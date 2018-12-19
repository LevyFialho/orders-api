using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.ClientApplicationAggregate
{
    public enum ClientApplicationStatus
    {
        Accepted = 1,
        Active = 2,
        Rejected = 3
    }
}
