using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class AggregateStateMismatchException: System.Exception
    {
        public AggregateStateMismatchException(string msg) : base(msg)
        {
            
        }
        protected AggregateStateMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
