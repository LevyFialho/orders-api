using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class AggregateEventOnApplyMethodMissingException: System.Exception
    {
        public AggregateEventOnApplyMethodMissingException(string msg) : base(msg)
        {
            
        }
        protected AggregateEventOnApplyMethodMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
