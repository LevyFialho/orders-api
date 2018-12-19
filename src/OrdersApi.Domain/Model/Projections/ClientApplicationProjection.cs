using System;
using System.Collections.Generic;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;

namespace OrdersApi.Domain.Model.Projections
{
    public class ClientApplicationProjection : Projection
    {
        public string ExternalKey { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ClientApplicationStatus Status { get; set; }
        
        public string RejectionReason { get; set; }
         
        public int Version { get; set; }

        public virtual List<ProductAccess> Products { get; set; }

        public ClientApplicationProjection()
        {
            Products = new List<ProductAccess>(); 
            Id = IdentityGenerator.NewSequentialIdentity();
        }

        public ClientApplicationProjection(ClientApplicationCreated @event) 
        {
            ExternalKey = @event.ExternalKey;
            SnapshotProjectionKey = @event.ExternalKey;
            Name = @event.Name;
            CreatedDate = @event.EventCommittedTimestamp;
            AggregateKey = @event.AggregateKey;
            Status = ClientApplicationStatus.Accepted;
            Version = @event.TargetVersion + 1;
            Products = new List<ProductAccess>();
            Id = IdentityGenerator.NewSequentialIdentity();
        }

        public virtual void Update(ClientApplicationActivated @event)
        {
            Status = ClientApplicationStatus.Active;
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ClientApplicationCreationRevoked @event)
        {
            RejectionReason = @event.Reason;
            Status = ClientApplicationStatus.Rejected;
            Version = @event.TargetVersion + 1;
        }

        public virtual void Update(ProductAccessUpdated @event)
        {
            Products.UpdateAccess(@event.ProductAccess);
            Version = @event.TargetVersion + 1;
        } 
    }
}
