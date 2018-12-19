using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Filters;
using FluentValidation.Results;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OrdersApi.Application.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class FluentValidationResultExtensions
    {
        public static JsonErrorResponse ToJsonErrorResponse(this ValidationResult result)
        {
            var list = new List<JsonError>();
            var response = new JsonErrorResponse();
            if(result?.Errors != null && result.Errors.Any())
                list.AddRange(result.Errors.Select(x => new JsonError()
                {
                    Code = 400,
                    Message = x.PropertyName + " " + x.ErrorMessage
                }));
            response.Errors = list.ToArray();
            return response;
        }
    }
}
