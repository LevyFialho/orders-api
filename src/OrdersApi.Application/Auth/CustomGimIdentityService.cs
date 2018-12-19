using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using OrdersApi.Authentication;
using AuthenticationOptions = OrdersApi.Authentication.AuthenticationOptions;

namespace OrdersApi.Application.Auth
{
    [ExcludeFromCodeCoverage]
    public class CustomGimIdentityService : MyIdentityService
    {
        private readonly IQueryBus _queryBus;

        public CustomGimIdentityService(IQueryBus queryBus, IOptionsMonitor<AuthenticationOptions> options) : base(options)
        {
            _queryBus = queryBus;
        } 

        protected override AuthenticationTicket BuildAuthenticationTicket(ClaimsIdentity identity)
        {
            var clientApplicationKey = identity.FindFirst(ClaimTypes.System).Value;

            var query = ClientApplicationSpecifications.FromCacheByExternalKey(clientApplicationKey);

            var clientApplicationProjection =  _queryBus.Send<SnapshotQuery<ClientApplicationProjection>, ClientApplicationProjection>(query)?.Result;

            if (clientApplicationProjection != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, clientApplicationProjection.Name));
            }

            return base.BuildAuthenticationTicket(identity);
        }
    }
}
