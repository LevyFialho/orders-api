using System.Diagnostics.CodeAnalysis;
using OrdersApi.Cqrs.Events; 

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class EventData: Event
    {
        public EventData()
        {
            State = EventState.NotPublished;
            TimesSent = 0;  
        }  
        public string Payload { get; set; } 
        public int TimesSent { get; set; } 
        public EventState State { get; set; }
    }
}
