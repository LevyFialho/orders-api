using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrdersApi.Application.Controllers.v1;
using OrdersApi.Application.Filters;
using OrdersApi.Contracts.V1.ClientApplication.Commands;
using OrdersApi.Contracts.V1.ClientApplication.Queries;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Commands.ClientApplication;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using CreateClientApplication = OrdersApi.Domain.Commands.ClientApplication.CreateClientApplication;
using Domain = OrdersApi.Domain;

namespace OrdersApi.UnitTests.Application.v1
{
    public class ClientApplicationControllerTests
    {
        public class PostTests
        {
            [Fact]
            public async Task PostClientApplicationTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<OrdersApi.Contracts.V1.ClientApplication.Commands.CreateClientApplication>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                string externalId = IdentityGenerator.NewSequentialIdentity();
                string name = "MyApp";
                request.Setup(x => x.GetCommand()).Returns(new CreateClientApplication(aggregateKey, correlationKey, applicationKey, externalId, name, sagaProcessKey));

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateClientApplication>()), Times.Once);
                var result = response as CreatedResult;
                Assert.NotNull(result);
                Assert.Equal(201, result.StatusCode);
                Assert.Equal(aggregateKey, (string)result.Value);
            }

            [Fact]
            public async Task PostClientApplicationInvalidCommandTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<OrdersApi.Contracts.V1.ClientApplication.Commands.CreateClientApplication>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var command = new Mock<CreateClientApplication>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                request.Setup(x => x.GetCommand()).Returns(command.Object);

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateClientApplication>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result);
            }


            [Fact]
            public async Task PostClientApplicationUnauthorized()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<OrdersApi.Contracts.V1.ClientApplication.Commands.CreateClientApplication>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false); 

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateClientApplication>()), Times.Never);
                var result = response as UnauthorizedResult;
                Assert.NotNull(result);
            }

            [Fact]
            public async Task PostClientApplicationDuplicateTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var existingApps = new List<ClientApplicationProjection>() { new ClientApplicationProjection() };
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult((IEnumerable<ClientApplicationProjection>)existingApps));

                var request = new Mock<OrdersApi.Contracts.V1.ClientApplication.Commands.CreateClientApplication>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                string externalId = IdentityGenerator.NewSequentialIdentity();
                string name = "MyApp";
                request.Setup(x => x.GetCommand()).Returns(new CreateClientApplication(aggregateKey, correlationKey, applicationKey, externalId, name, sagaProcessKey));

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateClientApplication>()), Times.Never);
                var result = response as ConflictObjectResult;
                Assert.NotNull(result);
                Assert.Equal(409, result.StatusCode);
            }

            [Fact]
            public async Task PostClientApplicationDuplicateExceptionTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<OrdersApi.Contracts.V1.ClientApplication.Commands.CreateClientApplication>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string externalId = IdentityGenerator.NewSequentialIdentity();
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                string name = "MyApp";
                request.Setup(x => x.GetCommand()).Returns(new CreateClientApplication(aggregateKey, correlationKey, applicationKey, externalId, name, sagaProcessKey));
                messageBus.Setup(x => x.SendCommand(It.IsAny<CreateClientApplication>()))
                    .Throws(new DuplicateException() { OrignalAggregateKey = aggregateKey });
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateClientApplication>()), Times.Once);
                var result = response as ConflictObjectResult;
                Assert.NotNull(result);
                Assert.Equal(409, result.StatusCode);
                Assert.Equal(aggregateKey, result.Value);
            }
        }

        public class PostProductAccessTests
        {
            [Fact]
            public async Task PostProductAccessTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                var app = new Mock<ClientApplicationProjection>();
                app.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
                var product = new Mock<ProductProjection>();
                var productKey = "Key";
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
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
                var canCharge = false;
                var canQuery = false;
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N"); 
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity(); 
                request.Setup(x => x.GetCommand()).Returns(new UpdateProductAccess(aggregateKey, correlationKey, applicationKey, productKey, canCharge, canQuery, sagaProcessKey));

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey, request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Once);
                var result = response as CreatedResult;
                Assert.NotNull(result);
                Assert.Equal(201, result.StatusCode);
                Assert.Equal(aggregateKey, (string)result.Value);
            }
             
            [Fact]
            public async Task PostProductAccessInvalidInternalKeyTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                var app = new Mock<ClientApplicationProjection>();
                app.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
              
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = string.Empty; 
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey, request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Never);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Never);
                var result = response as BadRequestResult;
                Assert.NotNull(result);
                Assert.Equal(400, result.StatusCode); 
            }

            [Fact]
            public async Task PostProductAccessClientNotActiveTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                var app = new Mock<ClientApplicationProjection>();
                app.Object.Status = ClientApplicationStatus.Rejected;
                var product = new Mock<ProductProjection>();
                var productKey = "Key";
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
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
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                request.Setup(x => x.GetCommand()).Returns(new UpdateProductAccess(aggregateKey, correlationKey, applicationKey, productKey, false, true, sagaProcessKey));

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey, request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result);
                Assert.Equal(400, result.StatusCode);
            }

            [Fact]
            public async Task PostProductAccesProductNotActiveTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                var app = new Mock<ClientApplicationProjection>();
                app.Object.Status = ClientApplicationStatus.Rejected;
                var product = new Mock<ProductProjection>();
                var productKey = "Key";
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
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
                var canCharge = false;
                var canQuery = false;
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity(); 
                request.Setup(x => x.GetCommand()).Returns(new UpdateProductAccess(aggregateKey, correlationKey, applicationKey, productKey, canCharge, canQuery, sagaProcessKey));

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey, request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result);
                Assert.Equal(400, result.StatusCode);
            }

            [Fact]
            public async Task PostProductAccesProductNotFoundTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                var app = new Mock<ClientApplicationProjection>();
                app.Object.Status = ClientApplicationStatus.Rejected;
                
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
                    {
                        app.Object
                    }));
                queryBus.Setup(x =>
                    x.Send<ISpecification<ProductProjection>, IEnumerable<ProductProjection>>(
                        It.IsAny<ISpecification<ProductProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ProductProjection>)new List<ProductProjection>()
                    {
                        
                    }));
                var canCharge = false;
                var canQuery = false;
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                request.Setup(x => x.GetCommand()).Returns(new UpdateProductAccess(aggregateKey, correlationKey, applicationKey, "X", canCharge, canQuery, sagaProcessKey));

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey, request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result);
                Assert.Equal(404, ((JsonErrorResponse)result.Value).Errors.First().Code);
                Assert.Equal(400, result.StatusCode);
            }
            [Fact]
            public async Task PostProductAccessConflictTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                var app = new Mock<ClientApplicationProjection>();
                var productKey = "Key";
                request.Object.ProductInternalKey = productKey;  
                app.Setup(x => x.Products).Returns(new List<ProductAccess>()
                {
                    new ProductAccess()
                    {
                        ProductAggregateKey = productKey
                    }
                });
                app.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
                var product = new Mock<ProductProjection>();
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
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
                request.Object.ProductInternalKey = productKey;
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();  
                
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey, request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                request.Verify(x => x.GetCommand(), Times.Never);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Never);
                var result = response as ConflictObjectResult;
                Assert.NotNull(result);
                Assert.Equal(409, result.StatusCode); 
            }

            [Fact]
            public async Task PostProductAccessInvalidCommandTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var app = new Mock<ClientApplicationProjection>();
                app.Object.Status = ClientApplicationStatus.Active;
                var product = new Mock<ProductProjection>();
                var productKey = "Key";
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
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
                var request = new Mock<CreateProductAccess>();
                string aggregateKey = IdentityGenerator.NewSequentialIdentity();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var command = new Mock<UpdateProductAccess>(null, null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                request.Setup(x => x.GetCommand()).Returns(command.Object);

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey,request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result);
            }


            [Fact]
            public async Task PostProductAccessUnauthorized()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var request = new Mock<CreateProductAccess>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false);

                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess("internalKey", request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateClientApplication>()), Times.Never);
                var result = response as UnauthorizedResult;
                Assert.NotNull(result);
            } 

            [Fact]
            public async Task PostProductAccessDuplicateExceptionTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var app = new Mock<ClientApplicationProjection>();
                app.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
                var product = new Mock<ProductProjection>();
                var productKey = "Key";
                product.Object.AggregateKey = productKey;
                product.Object.Status = ProductStatus.Active;
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
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
                var request = new Mock<CreateProductAccess>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true); 
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");  
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                request.Setup(x => x.GetCommand()).Returns(new UpdateProductAccess(aggregateKey, correlationKey, 
                    applicationKey, productKey, false, false, sagaProcessKey));
                messageBus.Setup(x => x.SendCommand(It.IsAny<UpdateProductAccess>()))
                    .Throws(new DuplicateException() { OrignalAggregateKey = aggregateKey });
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.PostProductAccess(aggregateKey,request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<UpdateProductAccess>()), Times.Once);
                var result = response as ConflictObjectResult;
                Assert.NotNull(result);
                Assert.Equal(409, result.StatusCode);
                Assert.Equal(aggregateKey, result.Value);
            }
        }

        public class GetTests
        {
            [Fact]
            public async Task GetAllTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.SendPagedQuery<PagedQuery<ClientApplicationProjection>, ClientApplicationProjection>(
                        It.IsAny<PagedQuery<ClientApplicationProjection>>())).Returns(Task.FromResult(new PagedResult<ClientApplicationProjection>()
                        {
                            PageSize = 1,
                            CurrentPage = 2,
                            Items = new List<ClientApplicationProjection>() { new ClientApplicationProjection() },
                            TotalItems = 3
                        }));

                var request = new Mock<GetClientApplicationList>();
                request.Setup(x => x.PagedQuery()).Returns(new PagedQuery<ClientApplicationProjection>()).Verifiable();
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.GetList(request.Object);

                request.Verify(x => x.PagedQuery(), Times.Once);
                queryBus.Verify(x => x.SendPagedQuery<PagedQuery<ClientApplicationProjection>, ClientApplicationProjection>(It.IsAny<PagedQuery<ClientApplicationProjection>>()), Times.Once);
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
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
                    {
                        new ClientApplicationProjection()
                    }));
                var request = new Mock<GetClientApplication>();
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ClientApplicationProjection>(x => true)).Verifiable();
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Get(request.Object);
                request.Verify(x => x.Specification(), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
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
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
                    {

                    }));
                var request = new Mock<GetClientApplication>();
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ClientApplicationProjection>(x => true)).Verifiable();
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.Get(request.Object);

                request.Verify(x => x.Specification(), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
                var result = response as NotFoundResult;
                Assert.NotNull(result);
                Assert.Equal(404, result.StatusCode);
            }

        }

        public class GetProductAccessTests
        { 

            [Fact]
            public async Task GetTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>(); 
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
                    {
                        new ClientApplicationProjection()
                        {
                            Products = new List<ProductAccess>()
                            {
                                new ProductAccess()
                                {
                                    ProductAggregateKey = "x",
                                    CanQuery = true,
                                    CanCharge = false
                                }
                            }
                        }
                    }));
                var request = new GetClientProducts()
                {
                    CanQuery = true,
                    PageNumber = 1,
                    PageSize = 1
                }; 
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.GetProductAccess("X", request); 
                queryBus.Verify(x => x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
                var okResult = response as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.Equal(200, okResult.StatusCode);
            }

            [Fact]
            public async Task NotFoundTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(
                        It.IsAny<ISpecification<ClientApplicationProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ClientApplicationProjection>)new List<ClientApplicationProjection>()
                    {

                    }));
                var request = new GetClientProducts(); 
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.GetProductAccess("X", request);
                 
                queryBus.Verify(x => x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Once);
                var result = response as NotFoundResult;
                Assert.NotNull(result);
                Assert.Equal(404, result.StatusCode);
            }

            [Fact]
            public async Task BadRequestTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>(); 
                var request = new GetClientProducts();
                var controller = new ClientApplicationsController(messageBus.Object, queryBus.Object);

                var response = await controller.GetProductAccess(string.Empty, request);

                queryBus.Verify(x => x.Send<ISpecification<ClientApplicationProjection>, IEnumerable<ClientApplicationProjection>>(It.IsAny<ISpecification<ClientApplicationProjection>>()), Times.Never);
                var result = response as BadRequestResult;
                Assert.NotNull(result);
                Assert.Equal(400, result.StatusCode);
            }
        }

    }
}
