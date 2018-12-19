using System;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models;

namespace OrdersApi.Cqrs.Messages
{ 
    public class DomainNotification : Event
    {
        public string DomainNotificationKey { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }
        public int Version { get; private set; }

        public DomainNotification(string key, string value)
        {
            DomainNotificationKey = IdentityGenerator.NewSequentialIdentity();
            Version = 1;
            Key = key;
            Value = value;
        }
    }
}