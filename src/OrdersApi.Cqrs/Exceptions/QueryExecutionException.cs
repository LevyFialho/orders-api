using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class QueryExecutionException : System.Exception
    {
        public QueryExecutionException(string msg) : base(msg)
        {
            
        }

        protected QueryExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
