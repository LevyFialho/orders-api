using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Events.Product;
using OrdersApi.Domain.Model.Snapshots;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Domain.Model.ProductAggregate
{
    public class Product : AggregateRoot, ISnapshottable
    {
        public string ExternalKey { get; set; }

        public string Name { get; set; }
        
        public DateTime CreatedDate { get; set; }

        public virtual ProductStatus Status { get; set; }

        public string RejectionReason { get; set; }

        public List<AcquirerConfiguration> AcquirerConfigurations { get; set; }

        public Product()
        {
            AcquirerConfigurations = new List<AcquirerConfiguration>();
        }

        public Product(string aggregateKey, string correlationKey, string externalKey, string applicationKey, string name, string sagaProcessKey) : this()
        {
            AcquirerConfigurations = new List<AcquirerConfiguration>();
            ApplyEvent(new ProductCreated(aggregateKey, correlationKey, applicationKey, externalKey, name, CurrentVersion, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnCreated(ProductCreated @event)
        {
            CreatedDate = @event.EventCommittedTimestamp;
            ExternalKey = @event.ExternalKey;
            Name = @event.Name;
            AggregateKey = @event.AggregateKey;
            Status = ProductStatus.Accepted;
        }

        public virtual void Activate(string correlationKey, string applicationKey, string sagaProcessKey)
        {
            ApplyEvent(new ProductActivated(this.AggregateKey, correlationKey, applicationKey, CurrentVersion, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnActivated(ProductActivated @event)
        {
            Status = ProductStatus.Active;
        }

        public virtual void RevokeCreation(string correlationKey, string applicationKey, string reason, string sagaProcessKey)
        {
            ApplyEvent(new ProductCreationRevoked(this.AggregateKey, correlationKey, applicationKey, CurrentVersion, reason, sagaProcessKey));
        }

        [InternalEventHandler]
        public virtual void OnCreationRevoked(ProductCreationRevoked @event)
        {
            Status = ProductStatus.Rejected;
        }

        public virtual void SetAcquirerConfiguration(string correlationKey, string applicationKey, string sagaProcessKey, AcquirerConfiguration config)
        {
            ApplyEvent(new ProductAcquirerConfigurationUpdated(this.AggregateKey, correlationKey, applicationKey, CurrentVersion, sagaProcessKey, config));
        }

        [InternalEventHandler]
        public virtual void OnAcquirerConfigurationModified(ProductAcquirerConfigurationUpdated @event)
        {
            if (!string.IsNullOrWhiteSpace(@event?.Configuration?.AcquirerKey))
            {
                AcquirerConfigurations.RemoveAll(x => x.AcquirerKey == @event.Configuration.AcquirerKey);
                AcquirerConfigurations.Add(@event.Configuration);
            }
        }

        public Snapshot TakeSnapshot()
        {
            return new ProductSnapshot(IdentityGenerator.NewSequentialIdentity(), this);
        }

        public void ApplySnapshot(Snapshot snapshot)
        {
            if (!(snapshot is ProductSnapshot productSnapshot)) return;

            Name = productSnapshot.Name;
            AggregateKey = productSnapshot.AggregateKey;
            ExternalKey = productSnapshot.ExternalKey;
            CreatedDate = productSnapshot.CreatedDate;
            Status = productSnapshot.Status;
            RejectionReason = productSnapshot.RejectionReason;
            CurrentVersion = productSnapshot.Version;
            AcquirerConfigurations = productSnapshot.AcquirerConfigurations;
        }
    }
}
