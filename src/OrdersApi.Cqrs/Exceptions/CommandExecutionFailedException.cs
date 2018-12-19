using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace OrdersApi.Cqrs.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class CommandExecutionFailedException:System.Exception
    {
        public CommandExecutionFailedException(string msg) : base(msg)
        {
            
        }

        protected CommandExecutionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
