using System;
using System.Collections.Generic;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Model.ProductAggregate;

namespace OrdersApi.Domain.Model.Snapshots
{
    public class ProductSnapshot : Snapshot
    {
        public string ExternalKey { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public ProductStatus Status { get; set; }

        public string RejectionReason { get; set; }

        public List<AcquirerConfiguration> AcquirerConfigurations { get; set; }

        public ProductSnapshot()
        {

            AcquirerConfigurations = new List<AcquirerConfiguration>();
        }

        public ProductSnapshot(string snapshotKey, Product product) : base(snapshotKey, product.AggregateKey, product.CurrentVersion)
        { 
            Name = product.Name;
            ExternalKey = product.ExternalKey;
            CreatedDate = product.CreatedDate;
            Status = product.Status;
            AcquirerConfigurations = product.AcquirerConfigurations ?? new List<AcquirerConfiguration>();
            RejectionReason = product.RejectionReason;
        }
    }
}
