using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Extensions;
using OrdersApi.Application.Filters;
using OrdersApi.Contracts.V1.ClientApplication.Views;
using OrdersApi.Contracts.V1.Product.Commands;
using OrdersApi.Contracts.V1.Product.Queries;
using OrdersApi.Contracts.V1.Product.Views;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ProductProjections;
using OrdersApi.Domain.Specifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; 

namespace OrdersApi.Application.Controllers.v1
{
    /// <summary>
    /// V1 controller for the 'Product' resource
    /// </summary>
    [ApiVersion("1.0", Deprecated = false)]
    [Authorize]
    [Route("v{version:apiVersion}/[controller]")]
    public class ProductsController : Controller
    {
        private readonly IMessageBus _commandBus;
        private readonly IQueryBus _queryBus;

        public ProductsController(IMessageBus commandBus, IQueryBus queryBus)
        {
            _commandBus = commandBus;
            _queryBus = queryBus;
        } 
        
        [HttpPost]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post([FromBody]CreateProduct request)
        {
            if (!request.HasAdmnistratorRights()) //Authorize
                return Unauthorized();

            var command = request.GetCommand(); //Get cqrs command

            if (!command.IsValid()) //Validate
            {
                return BadRequest(command.ValidationResult.ToJsonErrorResponse());
            }

            var query = ProductSpecifications.ProjectionByExternalKey(request.ExternalId);

            var duplicate = await _queryBus.Send<Specification<ProductProjection>, IEnumerable<ProductProjection>>(query); //Query the read-model for duplicates

            if (duplicate.Any())
            {
                return Conflict((object) duplicate.FirstOrDefault(x => x.Status != ProductStatus.Rejected)?.AggregateKey);
            }

            try //Try to process the command synchronously
            { 
                await _commandBus.SendCommand(command);
            }
            catch (DuplicateException e)
            {
                return Conflict((object)e.OrignalAggregateKey);
            }
            
            return Created("v1/[controller]/{internalKey}", (object) command.AggregateKey);
        }

        [HttpGet("{internalKey}")]
        [ProducesResponseType(typeof(ProductView), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(GetProduct request)
        {
            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(request.Specification());

            if (apps == null || !apps.Any())
                return NotFound();

            return Ok(new ProductView(apps.First()));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductView>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetList(GetProductList request)
        {
            //Send query to bus
            var orders = await _queryBus.SendPagedQuery<PagedQuery<ProductProjection>, ProductProjection>(request.PagedQuery());

            return Ok(new PagedResult<ProductView>()
            {
                Items = orders.Items.Select(x => new ProductView(x)),
                PageSize = orders.PageSize,
                TotalItems = orders.TotalItems,
                CurrentPage = orders.CurrentPage
            });
        }

        [HttpPost("{internalKey}/acquirer-configurations")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostAcquirerConfiguration(string internalKey, [FromBody]CreateProductAcquirerConfiguration request)
        {
            if (string.IsNullOrWhiteSpace(internalKey))
                return BadRequest();

            if (!request.HasAdmnistratorRights())
                return Unauthorized();
            var errorList = new List<JsonError>();
            var productQuery = ProductSpecifications.ProjectionByAggregateKey(internalKey);

            var product = (await _queryBus.Send<ISpecification<ProductProjection>,
                IEnumerable<ProductProjection>>(productQuery)).FirstOrDefault(); //Query the read-model 

            if (product == null || product.Status != ProductStatus.Active)
                errorList.Add(new JsonError() { Code = 404, Message = "Active product not found." });

            if (errorList.Any())
                return BadRequest(new JsonErrorResponse()
                {
                    Errors = errorList.ToArray()
                });

            request.SetAggregateKey(internalKey);
            if (product?.AcquirerConfigurations?.FirstOrDefault(x => x.AcquirerKey == request.AcquirerKey) != null)
            {
                return Conflict(new JsonErrorResponse()
                {
                    Errors = new[]
                    {
                        new JsonError()
                        {
                            Code = 409,
                            Message = "Duplicate AcquirerKey"
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
                return Created("v1/products/{internalKey}", (object)command.AggregateKey);
            }
            catch (DuplicateException e)
            {
                return Conflict((object)e.OrignalAggregateKey);
            }

        }

        [HttpGet("{internalKey}/acquirer-configurations")]
        [ProducesResponseType(typeof(PagedResult<AcquirerConfigurationView>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(JsonErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductAcquirerConfigurations(string internalKey, GetProductAcquirerConfigurations request)
        {
            if (string.IsNullOrWhiteSpace(internalKey) || request == null)
                return BadRequest();

            //Send query to Bus
            var apps = await _queryBus.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(ProductSpecifications.ProjectionByAggregateKey(internalKey));

            Specification<AcquirerConfigurationProjection> specifications = new DirectSpecification<AcquirerConfigurationProjection>(x => true);
            if (!string.IsNullOrWhiteSpace(request?.Acquirerkey))
                specifications = specifications &&
                                 new DirectSpecification<AcquirerConfigurationProjection>(x =>
                                     x.AcquirerKey == request.Acquirerkey);

            if (!string.IsNullOrWhiteSpace(request?.AccountKey))
                specifications = specifications &&
                                 new DirectSpecification<AcquirerConfigurationProjection>(x =>
                                     x.AccountKey == request.AccountKey);

            if (request?.CanCharge != null)
                specifications = specifications &&
                                 new DirectSpecification<AcquirerConfigurationProjection>(x =>
                                     x.CanCharge == request.CanCharge);

            if (apps == null || !apps.Any())
                return NotFound();
            var list = apps.First().AcquirerConfigurations.Where(specifications.SatisfiedBy().Compile()).Skip(request.PageNumber - 1).Take(request.PageSize)
                .Select(x => new AcquirerConfigurationView(x)).ToList();
             


            return Ok(new PagedResult<AcquirerConfigurationView>()
            {
                PageSize = request.PageSize,
                CurrentPage = request.PageNumber,
                TotalItems = list?.Count ?? 0,
                Items = list  
            });
        }

    }
}
