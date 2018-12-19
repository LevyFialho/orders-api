using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class DuplicateException : System.AggregateException
    {
        public string OrignalAggregateKey { get; set; }

        public DuplicateException() 
        {
            
        }

        protected DuplicateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
