using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Extensions;
using OrdersApi.Application.Filters;
using OrdersApi.Contracts.V1.ClientApplication.Commands;
using OrdersApi.Contracts.V1.ClientApplication.Queries;
using OrdersApi.Contracts.V1.ClientApplication.Views;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OrdersApi.Application.Controllers.v1
{
    /// <summary>
    /// V1 controller for the 'ClientApplication' resource
    /// </summary>
    [ApiVersion("1.0", Deprecated = false)]
    [Authorize]
    [Route("v{version:apiVersion}/client-applications")]
    public class ClientApplicationsController : Controller
    {
        private readonly IMessageBus _commandBus;
        private readonly IQueryBus _queryBus;

        public ClientApplicationsController(IMessageBus commandBus, IQueryBus queryBus)
        {
            _commandBus = commandBus;
            _queryBus = queryBus;
        }

        [HttpPost]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post([FromBody]CreateClientApplication request)
        {
            if (!request.HasAdmnistratorRights())
                return Unauthorized();

            var command = request.GetCommand(); //Get cqrs command
            if (!command.IsValid()) //Validate
            {
                return BadRequest(command.ValidationResult.ToJsonErrorResponse());
            }
            var query = ClientApplicationSpecifications.ProjectionByExternalKey(request.ExternalId);
            var duplicate = await _queryBus.Send<ISpecification<ClientApplicationProjection>,
                IEnumerable<ClientApplicationProjection>>(query); //Query the read-model for duplicates
            if (duplicate.Any())
            {
                return Conflict((object)duplicate.FirstOrDefault(x => x.Status != ClientApplicationStatus.Rejected)?.AggregateKey);
            }
            try //Try to process the command synchronously
            {
                await _commandBus.SendCommand(command);
            }
            catch (DuplicateException e)
            {
                return Conflict((object)e.OrignalAggregateKey);
            }

            return Created("v1/client-applications/{internalKey}", (object)command.AggregateKey);
        }


        [HttpPost("{internalKey}/products")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostProductAccess(string internalKey, [FromBody]CreateProductAccess request)
        {
            if (string.IsNullOrWhiteSpace(internalKey))
                return BadRequest();

            if (!request.HasAdmnistratorRights())
                return Unauthorized();

            request.AggregateKey = internalKey;
            var query = ClientApplicationSpecifications.ProjectionByAggregateKey(request.AggregateKey);
            var productQuery = ProductSpecifications.ProjectionByAggregateKey(request.ProductInternalKey);
            var client = (await _queryBus.Send<ISpecification<ClientApplicationProjection>,
                IEnumerable<ClientApplicationProjection>>(query)).FirstOrDefault(); //Query the read-model 
            var errorList = new List<JsonError>();

            if (client == null || client.Status != ClientApplicationStatus.Active)
                errorList.Add(new JsonError() { Code = 404, Message = "Active client not found." });

            var product = (await _queryBus.Send<ISpecification<ProductProjection>,
                IEnumerable<ProductProjection>>(productQuery)).FirstOrDefault(); //Query the read-model 

            if (product == null || product.Status != ProductStatus.Active)
                errorList.Add(new JsonError() { Code = 404, Message = "Active product not found." });

            if (errorList.Any())
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = errorList.ToArray()
                });

            if (client?.Products?.FirstOrDefault(x => x.ProductAggregateKey == request.ProductInternalKey) != null)
            {
                return Conflict(new JsonErrorResponse()
                {
                    Errors = new[]
                    {
                        new JsonError()
                        {
                            Code = 409,
                            Message = "Duplicate ProductKey"
                        },
                    }
                });
            }

            try //Try to process the command synchronously
            {
                var command = request.GetCommand(); //Get cqrs command
                if (!command.IsValid()) //Validate
                {
                    return BadRequest(command.ValidationResult.ToJsonErrorResponse());
                }
                await _commandBus.SendCommand(command);
                return Created("v1/client-applications/{internalKey}", (object)command.AggregateKey);
            }
            catch (DuplicateException e)
            {
                return Conflict((object)e.OrignalAggregateKey);
            }

        }

        [HttpGet("{internalKey}/products")]
        [ProducesResponseType(typeof(PagedResult<ProductAccessView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductAccess(string internalKey, GetClientProducts request)
        {
            if (string.IsNullOrWhiteSpace(internalKey) || request == null)
                return BadRequest();

            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(ClientApplicationSpecifications.ProjectionByAggregateKey(internalKey));

            if (apps == null || !apps.Any())
                return NotFound();
            Specification<ProductAccess> specifications = new DirectSpecification<ProductAccess>(x => true);
            if (request?.CanCharge != null)
                specifications = specifications &&
                                 new DirectSpecification<ProductAccess>(x =>
                                     x.CanCharge == request.CanCharge);
            if (request?.CanQuery != null)
                specifications = specifications &&
                                 new DirectSpecification<ProductAccess>(x =>
                                     x.CanQuery == request.CanQuery);
            var list = apps.First().Products?.Where(specifications.SatisfiedBy().Compile()).Skip(request.PageNumber - 1).Take(request.PageSize)
                .Select(x => new ProductAccessView(x)).ToList();



            return Ok(new PagedResult<ProductAccessView>()
            {
                PageSize = request.PageSize,
                CurrentPage = request.PageNumber,
                TotalItems = list?.Count ?? 0,
                Items = list ?? new List<ProductAccessView>()
            });
        }

        [HttpGet("{internalKey}")]
        [ProducesResponseType(typeof(ClientApplicationView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(GetClientApplication request)
        {
            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(request.Specification());

            if (apps == null || !apps.Any())
                return NotFound();

            return Ok(new ClientApplicationView(apps.First()));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ClientApplicationView>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetList(GetClientApplicationList request)
        {
            //Send query to bus
            var orders = await _queryBus.SendPagedQuery<PagedQuery<ClientApplicationProjection>, ClientApplicationProjection>(request.PagedQuery());

            return Ok(new PagedResult<ClientApplicationView>()
            {
                Items = orders?.Items?.Select(x => new ClientApplicationView(x)) ?? new List<ClientApplicationView>(),
                PageSize = orders?.PageSize ?? request.PageSize,
                TotalItems = orders?.TotalItems ?? 0,
                CurrentPage = orders?.CurrentPage ?? 0
            });
        }

    }
}
