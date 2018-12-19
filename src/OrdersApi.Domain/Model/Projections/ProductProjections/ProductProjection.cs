using System;
using System.Collections.Generic;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections.ProductProjections;

// ReSharper disable once CheckNamespace
namespace OrdersApi.Domain.Model.Projections
{
    public class ProductProjection : Projection
    {
        public string ExternalKey { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public ProductStatus Status { get; set; }

        public List<AcquirerConfigurationProjection> AcquirerConfigurations { get; set; }

        public string RejectionReason { get; set; }

        public int Version { get; set; }

        public ProductProjection()
        {
            Id = IdentityGenerator.NewSequentialIdentity();
            AcquirerConfigurations = new List<AcquirerConfigurationProjection>();
        }

        public ProductProjection(ProductCreated @event)
        {
            ExternalKey = @event.ExternalKey;
            SnapshotProjectionKey = @event.ExternalKey;
            Name = @event.Name;
            CreatedDate = @event.EventCommittedTimestamp;
            AggregateKey = @event.AggregateKey;
            Status = ProductStatus.Accepted;
            Id = IdentityGenerator.NewSequentialIdentity();
            AcquirerConfigurations = new List<AcquirerConfigurationProjection>();
        }

        public virtual void Update(ProductActivated @event)
        {
            Status = ProductStatus.Active;
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ProductCreationRevoked @event)
        {
            RejectionReason = @event.Reason;
            Status = ProductStatus.Rejected;
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ProductAcquirerConfigurationUpdated @event)
        {
            AcquirerConfigurations.RemoveAll(x => x.AcquirerKey == @event.Configuration.AcquirerKey);
            AcquirerConfigurations.Add(new AcquirerConfigurationProjection(@event.Configuration));
            Version = @event.TargetVersion + 1;
        }
    }
}
