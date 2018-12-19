using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Controllers.v1;
using OrdersApi.Application.Filters;
using OrdersApi.Contracts.V1.ClientApplication.Commands;
using OrdersApi.Contracts.V1.Product.Queries;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.Commands.Product;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using CreateClientApplication = OrdersApi.Domain.Commands.ClientApplication.CreateClientApplication;

namespace OrdersApi.UnitTests.Application.v1
{
    public class ProductsControllerTests
    {
        public class Post
        {
            [Fact]
            public async void Should_return_Unauthorized_HttpStatus_when_request_HasAdministratorRights_is_false()
            {
                var command = new Mock<CreateProduct>(Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    "Test Product"); 

                var request = new Mock<OrdersApi.Contracts.V1.Product.Commands.CreateProduct>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false); 

                // Arrange
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();

                var productsController = new ProductsController(messageBus.Object, queryBus.Object);

                //Act
                var response = await productsController.Post(request.Object);

                //Assert
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                response.Should().BeOfType<UnauthorizedResult>();
            }

            [Fact]
            public async void Should_return_BadRequest_HttpStatus_when_invalid_request_data_is_recieved()
            {
                var command = new Mock<CreateProduct>(Guid.NewGuid().ToString(),
                                                             Guid.NewGuid().ToString(),
                                                             Guid.NewGuid().ToString(),
                                                             Guid.NewGuid().ToString(),
                                                             "Test Product", IdentityGenerator.NewSequentialIdentity());

                command.Setup(mock => mock.IsValid()).Returns(false);

                var request = new Mock<OrdersApi.Contracts.V1.Product.Commands.CreateProduct>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(mock => mock.GetCommand()).Returns(command.Object);

                // Arrange
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();

                var productsController = new ProductsController(messageBus.Object, queryBus.Object);

                //Act
                var response = await productsController.Post(request.Object);

                //Assert
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                response.Should().BeOfType<BadRequestObjectResult>();
            }

            [Fact]
            public async void Should_return_Conflct_HttpStatus_when_atempt_to_duplicate_entry()
            {
                // Arrange
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var list = new List<ProductProjection>()
                {
                    new ProductProjection(){ AggregateKey = aggregateKey }
                };

                queryBus.Setup(mock => mock.Send<Specification<ProductProjection>, IEnumerable<ProductProjection>>(It.IsAny<Specification<ProductProjection>>()))
                            .Returns(() => Task.FromResult(list.AsEnumerable()));

                var productsController = new ProductsController(messageBus.Object, queryBus.Object);
                var product = new OrdersApi.Contracts.V1.Product.Commands.CreateProduct()
                {
                    ExternalId = Guid.NewGuid().ToString(),
                    CorrelationKey = Guid.NewGuid(),
                    Name = "Test Product",
                };
                product.SetHasAdmnistratorRights(true);
                //Act
                var response = await productsController.Post(product);

                //Assert
                response.Should().BeOfType<ConflictObjectResult>();
                (response as ConflictObjectResult)?.Value.Should().Equals(aggregateKey);
            }

            [Fact]
            public async void Should_return_Conflict_HttpStatus_when_Duplicate_exception_is_throw()
            {
                // Arrange
                var messageBus = new Mock<IMessageBus>();

                messageBus.Setup(mock => mock.SendCommand(It.IsAny<CreateProduct>())).Throws<DuplicateException>();

                var queryBus = new Mock<IQueryBus>();

                var list = new List<ProductProjection>();

                queryBus.Setup(mock => mock.Send<Specification<ProductProjection>, IEnumerable<ProductProjection>>(It.IsAny<Specification<ProductProjection>>()))
                        .Returns(() => Task.FromResult(list.AsEnumerable()));

                var productsController = new ProductsController(messageBus.Object, queryBus.Object);
                var product = new OrdersApi.Contracts.V1.Product.Commands.CreateProduct()
                {
                    ExternalId = Guid.NewGuid().ToString(),
                    CorrelationKey = Guid.NewGuid(),
                    Name = "Test Product",
                };
                product.SetHasAdmnistratorRights(true);
                //Act
                var response = await productsController.Post(product);

                //Assert
                response.Should().BeOfType<ConflictObjectResult>();
            }

            [Fact]
            public async void Should_return_Create_HttpStatus_when_correct_data_is_recieved()
            {
                // Arrange
                var messageBus = new Mock<IMessageBus>();

                messageBus.Setup(mock => mock.SendCommand(It.IsAny<CreateProduct>())).Returns(() => Task.FromResult(0));

                var queryBus = new Mock<IQueryBus>();

                var list = new List<ProductProjection>();

                queryBus.Setup(mock => mock.Send<Specification<ProductProjection>, IEnumerable<ProductProjection>>(It.IsAny<Specification<ProductProjection>>()))
                        .Returns(() => Task.FromResult(list.AsEnumerable()));

                var productsController = new ProductsController(messageBus.Object, queryBus.Object);
                var product = new OrdersApi.Contracts.V1.Product.Commands.CreateProduct()
                {
                    ExternalId = Guid.NewGuid().ToString(),
                    CorrelationKey = Guid.NewGuid(),
                    Name = "Test Product",
                };
                product.SetHasAdmnistratorRights(true);
                //Act
                var response = await productsController.Post(product);

                //Assert
                response.Should().BeOfType<CreatedResult>();
            }
        }

        public class Get
        {

            [Fact]
            public async Task GetAllTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.SendPagedQuery<PagedQuery<ProductProjection>, ProductProjection>(
                        It.IsAny<PagedQuery<ProductProjection>>())).Returns(Task.FromResult(new PagedResult<ProductProjection>()
                        {
                            PageSize = 1,
                            CurrentPage = 2,
                            Items = new List<ProductProjection>() { new ProductProjection() },
                            TotalItems = 3
                        }));

                var request = new Mock<GetProductList>();
                request.Setup(x => x.PagedQuery()).Returns(new PagedQuery<ProductProjection>()).Verifiable();
                var controller = new ProductsController(messageBus.Object, queryBus.Object);

                var response = await controller.GetList(request.Object);

                request.Verify(x => x.PagedQuery(), Times.Once);
                queryBus.Verify(x => x.SendPagedQuery<PagedQuery<ProductProjection>, ProductProjection>(It.IsAny<PagedQuery<ProductProjection>>()), Times.Once);
                var okResult = response as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.Equal(200, okResult.StatusCode);
            }

            [Fact]
            public async Task GetByIdTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(
                        It.IsAny<ISpecification<ProductProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ProductProjection>)new List<ProductProjection>()
                    {
                        new ProductProjection()
                    }));
                var request = new Mock<GetProduct>();
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ProductProjection>(x => true)).Verifiable();
                var controller = new ProductsController(messageBus.Object, queryBus.Object);

                var response = await controller.Get(request.Object);
                request.Verify(x => x.Specification(), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(It.IsAny<ISpecification<ProductProjection>>()), Times.Once);
                var okResult = response as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.Equal(200, okResult.StatusCode);
            }

            [Fact]
            public async Task GetByIdNotFoundTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(
                        It.IsAny<ISpecification<ProductProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ProductProjection>)new List<ProductProjection>()
                    {

                    }));
                var request = new Mock<GetProduct>();
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ProductProjection>(x => true)).Verifiable();
                var controller = new ProductsController(messageBus.Object, queryBus.Object);

                var response = await controller.Get(request.Object);

                request.Verify(x => x.Specification(), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(It.IsAny<ISpecification<ProductProjection>>()), Times.Once);
                var result = response as NotFoundResult;
                Assert.NotNull(result);
                Assert.Equal(404, result.StatusCode);
            }
        }

        public class PostAcquirerConfigurationTests
        {
            [Fact]
            public async Task PostAcquirerConfigurationTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<OrdersApi.Contracts.V1.Product.Commands.CreateProductAcquirerConfiguration>();
                var app = new Mock<ProductProjection>(); 
                var product = new Mock<ProductProjection>();
                var productKey = "Key";
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(
                        It.IsAny<ISpecification<ProductProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ProductProjection>)new List<ProductProjection>()
                    {
                        app.Object
                    }));
                queryBus.Setup(x =>
                    x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(
                        It.IsAny<ISpecification<ProductProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ProductProjection>)new List<ProductProjection>()
                    {
                        product.Object
                    }));
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var command = new Mock<UpdateProductAcquirerConfiguration>(null,null,null,null,null,null,null);
                command.Setup(x => x.IsValid()).Returns(true);
                request.Setup(x => x.GetCommand()).Returns(command.Object);

                var controller = new ProductsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostAcquirerConfiguration(productKey, request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAcquirerConfiguration>()), Times.Once);
                var result = response as CreatedResult;
                Assert.NotNull(result);
                Assert.Equal(201, result.StatusCode); 
            }
            
        }

    }
}
