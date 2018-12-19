using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Extensions;
using OrdersApi.Application.Filters;
using OrdersApi.Contracts.V1.Charge.Commands;
using OrdersApi.Contracts.V1.Charge.Queries;
using OrdersApi.Contracts.V1.Charge.Views;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#pragma warning disable S1075

namespace OrdersApi.Application.Controllers.v1
{
    /// <summary>
    /// V1 controller for the 'ClientApplication' resource
    /// </summary>
    [Authorize]
    [ApiVersion("1.0", Deprecated = false)]
    [Route("v{version:apiVersion}/charges")]
    public class ChargesController : Controller
    {
        private readonly IMessageBus _commandBus;
        private readonly IQueryBus _queryBus;

        public ChargesController(IMessageBus commandBus, IQueryBus queryBus)
        {
            _commandBus = commandBus;
            _queryBus = queryBus;
        }

        #region Charges Methods

        [HttpPost("acquirer-account")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post([FromBody]CreateAcquirerAccountCharge request)
        {
            //Validate product being charged
            var product = await GetProduct(request.ProductExternalKey);

            if (product == null || product.Status != ProductStatus.Active)
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[] { new JsonError() { Code = 400, Message = "Invalid Product Key" } }
                });

            request.SetInternalProductKey(product.AggregateKey);
            var externalClientAppKey = request.ExternalClientApplicationId();

            //Check client authorization
            var client = await GetClient(externalClientAppKey);

            var authorized = request.HasAdmnistratorRights() || (client != null &&
                             client.Status == ClientApplicationStatus.Active &&
                             client.Products.Any(x => x.CanCharge && x.ProductAggregateKey == product.AggregateKey));

            if (!authorized)
                return Unauthorized();

            request.SetInternalApplicationKey(client?.AggregateKey ?? IdentityGenerator.DefaultApplicationKey());

            var command = request?.GetCommand(); //Get cqrs command

            if (command == null || !command.IsValid())
                return BadRequest(command?.ValidationResult.ToJsonErrorResponse());

            if (!string.IsNullOrWhiteSpace(request?.Payment?.AcquirerKey) && (product?.AcquirerConfigurations == null || !product.AcquirerConfigurations.Any(x => x.AcquirerKey == request.Payment.AcquirerKey && x.CanCharge)))
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[] { new JsonError() { Code = 400, Message = "Acquirer key is not valid for this product" } }
                });

            try //Try to process the command synchronously
            {
                await _commandBus.SendCommand(command);
            }
            catch (DuplicateException e)
            {
                return Conflict((object)e.OrignalAggregateKey);
            }
            catch (AggregateNotFoundException)
            {
                return NotFound();
            }
            catch (CommandExecutionFailedException ex)
            {
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[] { new JsonError() { Code = 400, Message = ex.Message } }
                });
            }

            return Created("v1/charges/" + command.AggregateKey, (object)command.AggregateKey);
        }

        [HttpGet("{internalKey}")]
        [ProducesResponseType(typeof(AcquirerAccountChargeView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(GetCharge request)
        {
            //Filter authorized products
            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();

                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }


            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(request.Specification());

            if (apps == null || !apps.Any())
                return NotFound();

            return Ok(new AcquirerAccountChargeView(apps.First()));
        }

        [HttpGet("{internalKey}/history")]
        [ProducesResponseType(typeof(PagedResult<ChargeStatusView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatusHistory(string internalKey, GetChargeHistory request)
        {
            if (string.IsNullOrWhiteSpace(internalKey))
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[]{new JsonError()
                {
                    Message = "Invalid internal key",
                    Code = 400
                } }
                });

            request.SetInternalChargeOrderKey(internalKey);

            //Filter authorized products
            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();

                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }


            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(request.Specification());

            if (apps == null || !apps.Any())
                return NotFound();

            var list = new List<ChargeStatusView>();
            if (apps.First().History?.Count > 0)
                list.AddRange(apps.First().History.Skip(request.PageNumber - 1).Take(request.PageSize).Select(x => new ChargeStatusView(x)));

            return Ok(new PagedResult<ChargeStatusView>()
            {
                CurrentPage = request.PageNumber,
                Items = list,
                PageSize = request.PageSize,
                TotalItems = apps.First().History?.Count ?? 0
            });
        }

        [HttpGet("stream")]
        [ProducesResponseType(typeof(SeekResult<AcquirerAccountChargeView>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Seek(SeekChargeList request)
        {

            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())//Filter authorized products
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();
                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }


            var orders = await _queryBus.SendSeekQuery<SeekQuery<ChargeProjection>, ChargeProjection>(request.PagedQuery()); //Send query to bus
            var result = orders?.Items?.Select(x => new AcquirerAccountChargeView(x)).ToList() ?? new List<AcquirerAccountChargeView>();

            return Ok(new SeekResult<AcquirerAccountChargeView>()
            {
                Items = result,
                PageSize = orders?.PageSize ?? request.PageSize, 
                CurrentIndexOffset = request?.Offset,
                NextIndexOffset = result?.LastOrDefault()?.InternalKey
            });
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AcquirerAccountChargeView>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetList(GetChargeList request)
        {

            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())//Filter authorized products
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();
                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }
            try
            {
                var orders = await _queryBus.SendPagedQuery<PagedQuery<ChargeProjection>, ChargeProjection>(request.PagedQuery()); //Send query to bus
                var result = orders?.Items?.Select(x => new AcquirerAccountChargeView(x)) ?? new List<AcquirerAccountChargeView>();

                return Ok(new PagedResult<AcquirerAccountChargeView>()
                {
                    Items = result,
                    PageSize = orders?.PageSize ?? request.PageSize,
                    TotalItems = orders?.TotalItems ?? 0,
                    CurrentPage = request.PageNumber,
                });
            }
            catch (QueryExecutionException)
            {
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[]{new JsonError()
                    {
                        Message = "Query resulted in too many results, consider using the Streaming endpoint",
                        Code = 400
                    } }
                });
            }
            
        }

        #endregion

        #region Reversals Methods

        [HttpPost("{internalKey}/reversals")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostReversal(string internalKey, [FromBody]RevertCharge request)
        {
            var externalClientAppKey = request.ExternalClientApplicationId();

            var charge = await GetCharge(internalKey);

            if (charge == null) return NotFound();
            request.SetChargeKey(charge.AggregateKey);

            //Check client authorization
            var client = await GetClient(externalClientAppKey);
            var authorized = (request.HasAdmnistratorRights() || (client != null &&
                             client.Status == ClientApplicationStatus.Active && charge.ClientApplication?.ExternalKey == client.ExternalKey));

            if (!authorized)
                return Unauthorized();

            request.SetInternalApplicationKey(client?.AggregateKey ?? IdentityGenerator.DefaultApplicationKey());

            var command = request.GetCommand(); //Get cqrs command

            if (!command.IsValid())
                return BadRequest(command.ValidationResult.ToJsonErrorResponse());

            try //Try to process the command synchronously
            {
                await _commandBus.SendCommand(command);
            }
            catch (DuplicateException e)
            {
                return Conflict((object)e.OrignalAggregateKey);
            }

            return Created("v1/charges/" + internalKey + "/reversals/" + command.AggregateKey, (object)command.ReversalKey);
        }



        [HttpGet("{internalKey}/reversals")]
        [ProducesResponseType(typeof(PagedResult<ChargeReversalView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetReversalList(string internalKey, GetChargeReversals request)
        {
            if (string.IsNullOrWhiteSpace(internalKey))
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[]{new JsonError()
                {
                    Message = "Invalid internal key",
                    Code = 400
                } }
                });

            request.SetInternalChargeOrderKey(internalKey);

            //Filter authorized products
            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();

                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }


            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(request.Specification());

            if (apps == null || !apps.Any())
                return NotFound();

            var list = new List<ChargeReversalView>();
            if (apps.First().History?.Count > 0)
                list.AddRange(apps.First().Reversals.Skip(request.PageNumber - 1).Take(request.PageSize).Select(x => new ChargeReversalView(x)));

            return Ok(new PagedResult<ChargeReversalView>()
            {
                CurrentPage = request.PageNumber,
                Items = list,
                PageSize = request.PageSize,
                TotalItems = apps.First().History?.Count ?? 0
            });
        }

        [HttpGet("{internalKey}/reversals/{reversalKey}/history")]
        [ProducesResponseType(typeof(PagedResult<ChargeStatusView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetReversalsStatusHistory(string internalKey, string reversalKey, GetReversalHistory request)
        {
            if (string.IsNullOrWhiteSpace(internalKey) || string.IsNullOrWhiteSpace(reversalKey))
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = new[]{new JsonError()
                {
                    Message = "Invalid internal/reversal key",
                    Code = 400
                } }
                });

            request.SetInternalChargeOrderKey(internalKey);
            request.SetInternalReversalKey(reversalKey);
            //Filter authorized products
            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();

                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }


            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(request.Specification());
            var reversal = apps.FirstOrDefault()?.Reversals?.FirstOrDefault(x => x.ReversalKey == reversalKey);
            if (reversal == null)
                return NotFound();

            var list = new List<ChargeStatusView>();
            if (reversal.History?.Count > 0)
                list.AddRange(reversal.History.Skip(request.PageNumber - 1).Take(request.PageSize).Select(x => new ChargeStatusView(x)));

            return Ok(new PagedResult<ChargeStatusView>()
            {
                CurrentPage = request.PageNumber,
                Items = list,
                PageSize = request.PageSize,
                TotalItems = reversal.History?.Count ?? 0
            });
        }

        [HttpGet("{internalKey}/reversals/{reversalKey}")]
        [ProducesResponseType(typeof(ChargeReversalView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetReversal(string reversalkey, GetCharge request)
        {
            //Filter authorized products
            if (!request.HasAdmnistratorRights() && !request.HasGlobalQueryRights())
            {
                var client = await GetClient(request.ExternalClientApplicationId());
                if (client == null || client.Status != ClientApplicationStatus.Active)
                    return Unauthorized();

                request.SetInternalApplicationKey(client.AggregateKey);
                request.SetProductInternalKeyAuthorizedList(client.Products.Select(x => x.ProductAggregateKey).ToArray());
            }


            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(request.Specification());
            var reversal = apps?.FirstOrDefault()?.Reversals?.FirstOrDefault(x => x.ReversalKey == reversalkey);
            if (reversal == null)
                return NotFound();

            return Ok(new ChargeReversalView(reversal));
        }

        #endregion

        #region Support methods

        protected async Task<ClientApplicationProjection> GetClient(string externalApplicationId)
        {
            var client = (await _queryBus.Send<SnapshotQuery<ClientApplicationProjection>,
                ClientApplicationProjection>(ClientApplicationSpecifications.FromCacheByExternalKey(externalApplicationId)));
            return client;
        }

        protected async Task<ChargeProjection> GetCharge(string chargeKey)
        {
            var result = await _queryBus.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(ChargeSpecifications.ProjectionByAggregateKey(chargeKey));
            return result.FirstOrDefault();
        }

        protected async Task<ProductProjection> GetProduct(string externalProductId)
        {
            var product = (await _queryBus.Send<SnapshotQuery<ProductProjection>,
                ProductProjection>(ProductSpecifications.ProjectionSnapshot(externalProductId)));
            return product;
        }

        #endregion

    }
}
