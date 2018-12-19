using System.Diagnostics.CodeAnalysis;

namespace OrdersApi.IntegrationServices.LegacyService.Contracts
{
    [ExcludeFromCodeCoverage]
    public class ErrorResponse
    { 
        public Error[] Errors { get; set; } 
    }
    [ExcludeFromCodeCoverage]
    public class Error
    {
        public int Code { get; set; }

        public string Message { get; set; }
    }
}
