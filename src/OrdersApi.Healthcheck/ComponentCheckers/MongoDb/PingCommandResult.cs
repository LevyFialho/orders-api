using MongoDB.Bson.Serialization.Attributes;

namespace OrdersApi.Healthcheck.ComponentCheckers.MongoDb
{
    public class PingCommandResult
    {
        [BsonElement("ok")]
        public bool Ok { get; set; }
    }
}
