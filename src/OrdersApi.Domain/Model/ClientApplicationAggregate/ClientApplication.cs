using System;
using System.Collections.Generic;
using System.Linq;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Events.ClientApplication;
using OrdersApi.Domain.Model.Snapshots;

namespace OrdersApi.Domain.Model.ClientApplicationAggregate
{
    public class ClientApplication : AggregateRoot, ISnapshottable
    {
        public string ExternalKey { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual ClientApplicationStatus Status { get; set; }

        public string RejectionReason { get; set; }

        public List<ProductAccess> Products { get; set; }

        public ClientApplication()
        {
            Products = new List<ProductAccess>();
        }

        public ClientApplication(string aggregateKey, string correlationKey, string externalKey, string applicationKey, string name, string sagaProcessKey) : this()
        {
            ApplyEvent(new ClientApplicationCreated(aggregateKey, correlationKey, applicationKey, externalKey, name, CurrentVersion, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnCreated(ClientApplicationCreated @event)
        {
            CreatedDate = @event.EventCommittedTimestamp;
            ExternalKey = @event.ExternalKey;
            Name = @event.Name;
            AggregateKey = @event.AggregateKey;
            Status = ClientApplicationStatus.Accepted;
        }

        public virtual void Activate(string correlationKey, string applicationKey, string sagaProcessKey)
        {
            ApplyEvent(new ClientApplicationActivated(this.AggregateKey, correlationKey, applicationKey, CurrentVersion, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnActivated(ClientApplicationActivated @event)
        {
            Status = ClientApplicationStatus.Active;
        }

        public virtual void UpdateProductAccess(string correlationKey, string applicationKey, ProductAccess access, string sagaProcessKey)
        {
            ApplyEvent(new ProductAccessUpdated(this.AggregateKey, correlationKey, applicationKey, CurrentVersion, access, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnProductAccessUpdated(ProductAccessUpdated @event)
        { 
            Products.UpdateAccess(@event.ProductAccess);
        }


        public virtual void Reject(string correlationKey, string applicationKey, string reason, string sagaProcessKey)
        {
            ApplyEvent(new ClientApplicationCreationRevoked(this.AggregateKey, correlationKey, applicationKey, CurrentVersion, reason, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnRejected(ClientApplicationCreationRevoked @event)
        {
            Status = ClientApplicationStatus.Rejected;
        }

        public Snapshot TakeSnapshot()
        {
            return new ClientApplicationSnapshot(IdentityGenerator.NewSequentialIdentity(), this);
        }

        public void ApplySnapshot(Snapshot snapshot)
        {
            if (!(snapshot is ClientApplicationSnapshot appSnapshot)) return;
            Name = appSnapshot.Name;
            AggregateKey = snapshot.AggregateKey;
            ExternalKey = appSnapshot.ExternalKey;
            CreatedDate = appSnapshot.CreatedDate;
            Status = appSnapshot.Status;
            CurrentVersion = appSnapshot.Version;
            if (appSnapshot.Products != null)
            {
                Products.Clear();
                Products.AddRange(appSnapshot.Products);
            }
        }


    }
}
