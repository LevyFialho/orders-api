using System.Diagnostics.CodeAnalysis;

namespace OrdersApi.Application.Filters
{
    [ExcludeFromCodeCoverage]
    public class JsonErrorResponse
    { 
        public JsonError[] Errors { get; set; } 
    }

    [ExcludeFromCodeCoverage]
    public class JsonError
    {
        public int Code { get; set; }

        public string Message { get; set; }
    }
}
