using System;
using System.Threading.Tasks;

namespace OrdersApi.Cqrs.Commands
{
    public interface ICommandScheduler
    {
        Task RunNow<T>(T command) where T : ICommand; 
        Task RunDelayed<T>(TimeSpan span, T command) where T : ICommand; 
    }
}
