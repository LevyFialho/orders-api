using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace OrdersApi.Healthcheck.ComponentCheckers.MongoDb
{
    public class PingCommand : Command<PingCommandResult>
    {
        public override RenderedCommand<PingCommandResult> Render(IBsonSerializerRegistry serializerRegistry)
        {
            return new RenderedCommand<PingCommandResult>(new { ping = 1 }.ToBsonDocument(), serializerRegistry.GetSerializer<PingCommandResult>());
        }
    }
}
