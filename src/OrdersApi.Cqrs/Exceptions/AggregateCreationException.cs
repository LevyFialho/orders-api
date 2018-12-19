using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class AggregateCreationException: System.Exception
    {
        public AggregateCreationException(string msg) : base(msg)
        {
            
        }

        protected AggregateCreationException(SerializationInfo info, StreamingContext context): base(info, context)
        {
            
        }
    }
}
