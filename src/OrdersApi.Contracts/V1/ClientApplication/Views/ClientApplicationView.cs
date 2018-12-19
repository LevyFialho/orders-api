using System;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Domain.Model.Projections;

namespace OrdersApi.Contracts.V1.ClientApplication.Views
{
    [ExcludeFromCodeCoverage]
    public class ClientApplicationView
    {
        public string ExternalKey { get; set; }

        public string InternalKey { get; set; }

        public string Name { get; set; }

        public string Status { get; set; }

        public DateTime CreatedDate { get; set; }
       
        public ClientApplicationView()
        {
            
        }

        public ClientApplicationView(ClientApplicationProjection app)
        {
            ExternalKey = app.ExternalKey;
            InternalKey = app.AggregateKey;
            Name = app.Name;
            CreatedDate = app.CreatedDate;
            Status = app.Status.ToString();
            if(!string.IsNullOrWhiteSpace(app.RejectionReason))
                 Status += " - " + app.RejectionReason;
        }
    }
}
