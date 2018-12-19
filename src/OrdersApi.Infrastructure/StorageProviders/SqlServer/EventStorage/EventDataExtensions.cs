using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{ 
    public static class EventDataExtensions
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
       

        public static IEvent DeserializeEvent(this EventData data)
        { 
            var payload = (Payload)JsonConvert.DeserializeObject(StringCompressor.DecompressString(data.Payload), typeof(Payload), SerializerSettings);
            return (IEvent)JsonConvert.DeserializeObject(payload.EventData, Type.GetType(payload.EventType), SerializerSettings);
        }

        public static IEvent DeserializeEvent(this Payload payload)
        { 
            return (IEvent)JsonConvert.DeserializeObject(payload.EventData, Type.GetType(payload.EventType), SerializerSettings);
        }

        public static Payload GetPayload(this EventData data)
        {
            return (Payload)JsonConvert.DeserializeObject(StringCompressor.DecompressString(data.Payload), typeof(Payload), SerializerSettings); 
        }

        public static EventData ToEventData(this IEvent @event, Type aggregateType)
        {
            var data = JsonConvert.SerializeObject(@event, SerializerSettings);
            var payload = JsonConvert.SerializeObject(new Payload()
            {
                EventType = @event.GetType().AssemblyQualifiedName,
                AggregateType = aggregateType.AssemblyQualifiedName,
                EventData = data
            }, SerializerSettings);
            var eventData = new EventData
            {
                EventKey = @event.EventKey,
                AggregateKey = @event.AggregateKey,
                Payload = StringCompressor.CompressString(payload), 
                EventCommittedTimestamp = @event.EventCommittedTimestamp,
                ClassVersion = @event.ClassVersion,
                TargetVersion = @event.TargetVersion,
                ApplicationKey = @event.ApplicationKey,
                CorrelationKey = @event.CorrelationKey
            };
            return eventData;
        }


    }
}
