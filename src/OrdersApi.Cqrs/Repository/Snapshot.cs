using System;

namespace OrdersApi.Cqrs.Repository
{
    public class Snapshot
    {
        public string SnapshotKey { get; set; }
        public string AggregateKey { get; set; }
        public short Version { get; set; }

        public Snapshot()
        {
            
        }

        public Snapshot(string id, string aggregateKey, short version):base()
        {
            SnapshotKey = id;
            AggregateKey = aggregateKey;
            Version = version;
        }
    }
}
