using System;
using System.Diagnostics.CodeAnalysis;

namespace OrdersApi.Cqrs.Models
{
    public class Projection : Entity
    { 
        public string Id { get; set; }

        public string SnapshotProjectionKey { get; set; }
         

        [ExcludeFromCodeCoverage]
        public string SnapshotProjectionKeyPrefix()
        {
            return GetType().Name;
        }
    }
}
