using System;
using OrdersApi.Cqrs.Messages;
using MediatR;

namespace OrdersApi.Cqrs.Commands
{ 
    public interface ICommand: IRequest
    { 
        string  AggregateKey { get; }
        string CommandKey { get; set; }
        string ApplicationKey { get; }
        string CorrelationKey { get; }
        string SagaProcessKey { get; }
    }
}
