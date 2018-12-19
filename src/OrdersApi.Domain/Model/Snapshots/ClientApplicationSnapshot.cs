using System;
using System.Collections.Generic;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Model.ClientApplicationAggregate;

namespace OrdersApi.Domain.Model.Snapshots
{
    public class ClientApplicationSnapshot : Snapshot
    {
        public string ExternalKey { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public ClientApplicationStatus Status { get; set; }

        public List<ProductAccess> Products { get; set; }

        public ClientApplicationSnapshot()
        {
            
        }

        public ClientApplicationSnapshot(string snapshotKey, ClientApplication app) : base(snapshotKey, app.AggregateKey, app.CurrentVersion)
        { 
            Products = app.Products ?? new List<ProductAccess>();
            Name = app.Name;
            ExternalKey = app.ExternalKey;
            CreatedDate = app.CreatedDate;
            Status = app.Status;
        }
 
         
    }
}
