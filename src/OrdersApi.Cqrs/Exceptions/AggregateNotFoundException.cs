using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class AggregateNotFoundException: System.Exception
    {
        public AggregateNotFoundException(string msg) : base(msg)
        {
            
        }
        protected AggregateNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
