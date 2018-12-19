using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace OrdersApi.Cqrs.Models
{
    /// <summary>
    /// Value object representing an operation result
    /// </summary> 
    [ExcludeFromCodeCoverage]
    public class IntegrationResult : ValueObject<IntegrationResult>
    {
        #region Constructors
         
        public IntegrationResult()
        {
            Result = Result.Error;
            Details = new List<string>();
        } 

        public IntegrationResult(Result r)
        {
            Result = r;
            Details = new List<string>();
        }
         
        public IntegrationResult(Result r, IEnumerable<string> details)
        {
            Result = r;
            Details = details?.ToList() ?? new List<string>();
        }

        public IntegrationResult(Result r,   HttpStatusCode statusCode)
        {
            Result = r;
            StatusCode = statusCode;
        }

        public IntegrationResult(Result r, IEnumerable<string> details, HttpStatusCode statusCode)
        {
            Result = r;
            Details = details?.ToList() ?? new List<string>();
            StatusCode = statusCode;
        }

        #endregion

        public HttpStatusCode? StatusCode { get; set; }
         
        public Result Result { get; set; }
         
        public List<string> Details { get; set; }
         
        public bool Success => Result == Result.Sucess;
 
    }

    /// <summary>
    /// Type parameter Operation result value object
    /// </summary>
    /// <typeparam name="T">Expected returned object type</typeparam>
    [ExcludeFromCodeCoverage]
    public class IntegrationResult<T> : IntegrationResult
    {
        #region Constructors
         
        public IntegrationResult()
            : base()
        {

        } 

        public IntegrationResult(Result r)
            : base(r)
        {

        }
         
        public IntegrationResult(Result r, IEnumerable<string> details)
            : base(r, details)
        {

        }

        public IntegrationResult(Result r, HttpStatusCode statusCode)
            : base(r, statusCode)
        {

        }

        public IntegrationResult(Result r, IEnumerable<string> details, HttpStatusCode statusCode)
            : base(r, details, statusCode)
        {
            
        }

        #endregion

        /// <summary>
        /// Object returned by the operation
        /// </summary>
        public T ReturnedObject { get; set; }
    }

    /// <summary>
    /// Enumeration for result
    /// </summary>
    public enum Result
    {
        /// <summary>
        /// Error
        /// </summary>
        Error,

        /// <summary>
        /// Success
        /// </summary>
        Sucess
    }
     
}
