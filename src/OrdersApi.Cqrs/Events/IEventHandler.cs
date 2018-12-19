using System.Threading.Tasks;
using MediatR;

namespace OrdersApi.Cqrs.Events
{ 
    public interface IEventHandler<in T> : INotificationHandler<T> where T:IEvent
    {
      
    }
}
