using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConcurrencyException: System.Exception
    {
        public ConcurrencyException(string msg) : base(msg)
        {
            
        }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
