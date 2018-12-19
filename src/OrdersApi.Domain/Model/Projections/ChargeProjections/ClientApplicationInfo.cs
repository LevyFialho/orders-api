using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.Projections.ChargeProjections
{
    public class ClientApplicationInfo
    { 
        public static ClientApplicationInfo GetInfo(ClientApplicationProjection projection)
        {
            if (projection == null)
                return null;

            return new ClientApplicationInfo
            {
                ExternalKey = projection.ExternalKey,
                Name = projection.Name
            }; 
        } 

        public string ExternalKey { get; set; }

        public string Name { get; set; }
    }
}
