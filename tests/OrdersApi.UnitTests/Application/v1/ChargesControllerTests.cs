using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Application.Controllers.v1;
using OrdersApi.Contracts.V1.Charge.Queries;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Messages;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Domain.Model.Projections.ProductProjections;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Application.v1
{
    public class ChargesControllerTests
    {
        public class PostTests
        {
            [Fact]
            public async Task PostChargeWithAdminRightsTest()
            {
                var fixture = new Fixture();
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();

                
                var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.CreateAcquirerAccountCharge>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                var orderDetails = fixture.Create<OrderDetails>();
                orderDetails.ChargeDate = DateTime.UtcNow.AddHours(25); 
                orderDetails.Amount = 10;
                var paymentData = fixture.Create<AcquirerAccount>();
                var productProjections = new ProductProjection()
                {
                    Status = ProductStatus.Active,
                    AggregateKey = IdentityGenerator.NewSequentialIdentity(),
                    AcquirerConfigurations = new List<AcquirerConfigurationProjection>()
                    {
                        new AcquirerConfigurationProjection()
                        {
                            CanCharge = true,
                            AcquirerKey = paymentData.AcquirerKey,
                            AccountKey = IdentityGenerator.NewSequentialIdentity()
                        }
                    }
                };
                queryBus.Setup(x => x.Send<SnapshotQuery<ProductProjection>,
                    ProductProjection>(It.IsAny<SnapshotQuery<ProductProjection>>())).Returns(Task.FromResult(productProjections));
                request.Setup(x => x.GetCommand()).Returns(new CreateAcquirerAccountCharge(aggregateKey, correlationKey, applicationKey, sagaProcessKey, orderDetails, paymentData));

                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Once);
                var result = response as CreatedResult;
                Assert.NotNull(result);
                Assert.Equal(201, result.StatusCode);
                Assert.Equal(aggregateKey, (string)result.Value);
            }

            [Fact]
            public async Task PostChargeWithoutAdminRightsTest()
            { 
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();

                var productKey = IdentityGenerator.NewSequentialIdentity();
                var clientProjections = new ClientApplicationProjection()
                {
                    Status = ClientApplicationStatus.Active,
                    AggregateKey = IdentityGenerator.NewSequentialIdentity(),
                    Products = new List<ProductAccess>()
                    {
                        new ProductAccess()
                        {
                            CanCharge = true,
                            ProductAggregateKey = productKey
                        }
                    }
                };
                var productProjections = new ProductProjection()
                {
                    Status = ProductStatus.Active,
                    AggregateKey = productKey,
                    AcquirerConfigurations = new List<AcquirerConfigurationProjection>()
                    {
                        new AcquirerConfigurationProjection()
                        {
                            CanCharge = true, 
                            AccountKey = IdentityGenerator.NewSequentialIdentity()
                        }
                    }
                };
                queryBus.Setup(x => x.Send<SnapshotQuery<ProductProjection>,
                    ProductProjection>(It.IsAny<SnapshotQuery<ProductProjection>>())).Returns(Task.FromResult(productProjections));
                queryBus.Setup(x => x.Send<SnapshotQuery<ClientApplicationProjection>,
                    ClientApplicationProjection>(It.IsAny<SnapshotQuery<ClientApplicationProjection>>())).Returns(Task.FromResult(clientProjections));

                var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.CreateAcquirerAccountCharge>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                var command = new Mock<CreateAcquirerAccountCharge>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                command.Object.AggregateKey = IdentityGenerator.NewSequentialIdentity();
                request.Setup(x => x.GetCommand()).Returns(command.Object);
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Once);
                var result = response as CreatedResult;
                Assert.NotNull(result);
                Assert.Equal(201, result.StatusCode);
                Assert.Equal(command.Object.AggregateKey, (string)result.Value);
            }
            
            [Fact]
            public async Task InvalidClientProductAccessReturnsUnauthorized()
            { 
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var productKey = IdentityGenerator.NewSequentialIdentity();
                var productProjections = new ProductProjection()
                {
                    Status = ProductStatus.Active,
                    AggregateKey = productKey,
                    AcquirerConfigurations = new List<AcquirerConfigurationProjection>()
                    {
                        new AcquirerConfigurationProjection()
                        {
                            CanCharge = true, 
                            AccountKey = IdentityGenerator.NewSequentialIdentity()
                        }
                    }
                };
                queryBus.Setup(x => x.Send<SnapshotQuery<ProductProjection>,
                    ProductProjection>(It.IsAny<SnapshotQuery<ProductProjection>>())).Returns(Task.FromResult(productProjections));

                var clientProjections =  new ClientApplicationProjection()
                    {
                        Status = ClientApplicationStatus.Active,
                        AggregateKey = IdentityGenerator.NewSequentialIdentity(),
                        Products = new List<ProductAccess>()
                        {
                            new ProductAccess()
                            {
                                CanCharge = false,
                                ProductAggregateKey = productKey
                            }
                        }
                    };
                queryBus.Setup(x => x.Send<SnapshotQuery<ClientApplicationProjection>,
                    ClientApplicationProjection>(It.IsAny<SnapshotQuery<ClientApplicationProjection>>())).Returns(Task.FromResult(clientProjections));

                var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.CreateAcquirerAccountCharge>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                var command = new Mock<CreateAcquirerAccountCharge>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                request.Setup(x => x.GetCommand()).Returns(command.Object);

                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                queryBus.Verify(x => x.Send<SnapshotQuery<ProductProjection>,
                    ProductProjection>(It.IsAny<SnapshotQuery<ProductProjection>>()),
                    Times.Once);
                request.Verify(x => x.GetCommand(), Times.Never);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Never);
                var result = response as UnauthorizedResult; 
                Assert.NotNull(result);
            }

            [Fact]
            public async Task InvalidProductReturnsBadRequest()
            { 
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                IEnumerable<ProductProjection> productProjections = new List<ProductProjection>()
                {
                    new ProductProjection()
                    {
                        Status = ProductStatus.Rejected,
                        AggregateKey = IdentityGenerator.NewSequentialIdentity()
                    }
                };
                queryBus.Setup(x => x.Send<ISpecification<ProductProjection>,
                    IEnumerable<ProductProjection>>(It.IsAny<ISpecification<ProductProjection>>())).Returns(Task.FromResult(productProjections));
                var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.CreateAcquirerAccountCharge>();
                var command = new Mock<CreateAcquirerAccountCharge>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(true);
                request.Setup(x => x.GetCommand()).Returns(command.Object);

                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);
                request.Verify(x => x.HasAdmnistratorRights(), Times.Never);
                request.Verify(x => x.GetCommand(), Times.Never);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result); 
            }

            [Fact]
            public async Task PostChargeInvalidCommandTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();

                var productProjections = new ProductProjection()
                {
                    Status = ProductStatus.Active,
                    AggregateKey = IdentityGenerator.NewSequentialIdentity(),
                    AcquirerConfigurations = new List<AcquirerConfigurationProjection>()
                    {
                        new AcquirerConfigurationProjection()
                        {
                            CanCharge = true, 
                            AccountKey = IdentityGenerator.NewSequentialIdentity()
                        }
                    }
                };
                queryBus.Setup(x => x.Send<SnapshotQuery<ProductProjection>,
                    ProductProjection>(It.IsAny<SnapshotQuery<ProductProjection>>())).Returns(Task.FromResult(productProjections));
                var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.CreateAcquirerAccountCharge>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var command = new Mock<CreateAcquirerAccountCharge>(null, null, null, null, null, null);
                command.Setup(x => x.IsValid()).Returns(false);
                request.Setup(x => x.GetCommand()).Returns(command.Object);

                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                command.Verify(c => c.IsValid(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result);
            }

            [Fact]
            public async Task PostChargeDuplicateExceptionTest()
            {
                var fixture = new Fixture();
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var productProjections = new ProductProjection()
                {
                    Status = ProductStatus.Active,
                    AggregateKey = IdentityGenerator.NewSequentialIdentity(),
                    AcquirerConfigurations = new List<AcquirerConfigurationProjection>()
                    {
                        new AcquirerConfigurationProjection()
                        {
                            CanCharge = true, 
                            AccountKey = IdentityGenerator.NewSequentialIdentity()
                        }
                    }
                };
                queryBus.Setup(x => x.Send<SnapshotQuery<ProductProjection>,
                    ProductProjection>(It.IsAny<SnapshotQuery<ProductProjection>>())).Returns(Task.FromResult(productProjections));
                var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.CreateAcquirerAccountCharge>();
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                var applicationKey = IdentityGenerator.DefaultApplicationKey();
                var correlationKey = Guid.NewGuid().ToString("N");
                string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                var orderDetails = fixture.Create<OrderDetails>();
                orderDetails.ChargeDate = DateTime.UtcNow.AddHours(25); 
                orderDetails.Amount = 10;
                var paymentData = fixture.Create<AcquirerAccount>();
                request.Setup(x => x.GetCommand()).Returns(new CreateAcquirerAccountCharge(aggregateKey, correlationKey, applicationKey, sagaProcessKey, orderDetails, paymentData));

                messageBus.Setup(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()))
                    .Throws(new DuplicateException() { OrignalAggregateKey = aggregateKey });
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Post(request.Object);

                request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Once);
                var result = response as ConflictObjectResult;
                Assert.NotNull(result);
                Assert.Equal(409, result.StatusCode);
                Assert.Equal(aggregateKey, result.Value);
            }
        }

        public class GetTests
        {
            [Fact]
            public async Task GetListUnauthorizedTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.SendPagedQuery<PagedQuery<ChargeProjection>, ChargeProjection>(
                        It.IsAny<PagedQuery<ChargeProjection>>())).Returns(Task.FromResult(new PagedResult<ChargeProjection>()
                        {
                            PageSize = 1,
                            CurrentPage = 2,
                            Items = new List<ChargeProjection>() { new ChargeProjection() },
                            TotalItems = 3
                        }));

                var request = new Mock<GetChargeList>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(false);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                request.Setup(x => x.PagedQuery()).Returns(new PagedQuery<ChargeProjection>()).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.GetList(request.Object);

                request.Verify(x => x.PagedQuery(), Times.Never);
                queryBus.Verify(x => x.SendPagedQuery<PagedQuery<ChargeProjection>, ChargeProjection>(It.IsAny<PagedQuery<ChargeProjection>>()), Times.Never);
                var okResult = response as UnauthorizedResult;
                Assert.NotNull(okResult);
            }

            [Fact]
            public async Task GetListTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.SendPagedQuery<PagedQuery<ChargeProjection>, ChargeProjection>(
                        It.IsAny<PagedQuery<ChargeProjection>>())).Returns(Task.FromResult(new PagedResult<ChargeProjection>()
                        {
                            PageSize = 1,
                            CurrentPage = 2,
                            Items = new List<ChargeProjection>() { new ChargeProjection() },
                            TotalItems = 3
                        }));

                var request = new Mock<GetChargeList>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.PagedQuery()).Returns(new PagedQuery<ChargeProjection>()).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.GetList(request.Object);

                request.Verify(x => x.PagedQuery(), Times.Once);
                queryBus.Verify(x => x.SendPagedQuery<PagedQuery<ChargeProjection>, ChargeProjection>(It.IsAny<PagedQuery<ChargeProjection>>()), Times.Once);
                var okResult = response as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.Equal(200, okResult.StatusCode);
            }

            [Fact]
            public async Task SeekUnauthorizedTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.SendSeekQuery<SeekQuery<ChargeProjection>, ChargeProjection>(
                        It.IsAny<SeekQuery<ChargeProjection>>())).Returns(Task.FromResult(new SeekResult<ChargeProjection>()
                {
                    PageSize = 1,
                    Items = new List<ChargeProjection>() { new ChargeProjection() }, 
                }));

                var request = new Mock<SeekChargeList>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(false);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                request.Setup(x => x.PagedQuery()).Returns(new SeekQuery<ChargeProjection>()).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Seek(request.Object);

                request.Verify(x => x.PagedQuery(), Times.Never);
                queryBus.Verify(x => x.SendSeekQuery<SeekQuery<ChargeProjection>, ChargeProjection>(It.IsAny<SeekQuery<ChargeProjection>>()), Times.Never);
                var okResult = response as UnauthorizedResult;
                Assert.NotNull(okResult);
            }

            [Fact]
            public async Task SeekTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.SendSeekQuery<SeekQuery<ChargeProjection>, ChargeProjection>(
                        It.IsAny<SeekQuery<ChargeProjection>>())).Returns(Task.FromResult(new SeekResult<ChargeProjection>()
                {
                    PageSize = 1,
                    CurrentIndexOffset = "xpto",
                    Items = new List<ChargeProjection>() { new ChargeProjection() },
                    NextIndexOffset = "xpto2"
                }));

                var request = new Mock<SeekChargeList>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.PagedQuery()).Returns(new SeekQuery<ChargeProjection>()).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Seek(request.Object);

                request.Verify(x => x.PagedQuery(), Times.Once);
                queryBus.Verify(x => x.SendSeekQuery<SeekQuery<ChargeProjection>, ChargeProjection>(It.IsAny<SeekQuery<ChargeProjection>>()), Times.Once);
                var okResult = response as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.Equal(200, okResult.StatusCode);
            }

            [Fact]
            public async Task GetByIdTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var projection = new Fixture().Create<ChargeProjection>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                        It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                    {
                        projection
                    }));
                var request = new Mock<GetCharge>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Get(request.Object);
                request.Verify(x => x.Specification(), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
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
                    x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                        It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                    {

                    }));
                var request = new Mock<GetCharge>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.Get(request.Object);

                request.Verify(x => x.Specification(), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                var result = response as NotFoundResult;
                Assert.NotNull(result);
                Assert.Equal(404, result.StatusCode);
            }

        }

        public class GetHistoryTests
        {

            [Fact]
            public async Task GetHistoryByIdTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                var projection = new Fixture().Create<ChargeProjection>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                        It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                    {
                        projection
                    }));
                var request = new Mock<GetChargeHistory>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.GetStatusHistory("x", request.Object);
                request.Verify(x => x.Specification(), Times.Once);
                request.Verify(x => x.SetInternalChargeOrderKey(It.IsAny<string>()), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                var okResult = response as OkObjectResult;
                Assert.NotNull(okResult);
                Assert.Equal(200, okResult.StatusCode);
            }

            [Fact]
            public async Task GetHistoryNotFoundTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                        It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                    {

                    }));
                var request = new Mock<GetChargeHistory>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.GetStatusHistory("x", request.Object);

                request.Verify(x => x.Specification(), Times.Once);
                request.Verify(x => x.SetInternalChargeOrderKey(It.IsAny<string>()), Times.Once);
                queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                var result = response as NotFoundResult;
                Assert.NotNull(result);
                Assert.Equal(404, result.StatusCode);
            }


            [Fact]
            public async Task GetHistoryBadRequestTest()
            {
                var messageBus = new Mock<IMessageBus>();
                var queryBus = new Mock<IQueryBus>();
                queryBus.Setup(x =>
                    x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                        It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                    (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                    {

                    }));
                var request = new Mock<GetChargeHistory>();
                request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                var controller = new ChargesController(messageBus.Object, queryBus.Object);

                var response = await controller.GetStatusHistory(string.Empty, request.Object);

                request.Verify(x => x.Specification(), Times.Never);
                request.Verify(x => x.SetInternalChargeOrderKey(It.IsAny<string>()), Times.Never);
                queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Never);
                var result = response as BadRequestObjectResult;
                Assert.NotNull(result); 
            }
        }

        public class AcquirerAccountReversalsTests
        {
            public class PostTests
            {
                [Fact]
                public async Task PostReversaleWithAdminRightsTest()
                { 
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var orderMock = new Mock<ChargeProjection>();
                    queryBus.Setup(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                                It.IsAny<ISpecification<ChargeProjection>>()))
                        .Returns(Task.FromResult((IEnumerable<ChargeProjection>) new[] {orderMock.Object}));
                    var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.RevertCharge>();
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                    var applicationKey = IdentityGenerator.DefaultApplicationKey();
                    var correlationKey = Guid.NewGuid().ToString("N");
                    var reversalAmount = 50m;
                    var internalOrderKey = IdentityGenerator.NewSequentialIdentity();
                    string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                    var mockCommand = new Mock<CreateChargeReversal>(aggregateKey, correlationKey, applicationKey, sagaProcessKey, reversalAmount) { CallBase = true };
                    request.Setup(x => x.GetCommand()).Returns(mockCommand.Object);
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.PostReversal(internalOrderKey, request.Object);

                    request.Verify(x => x.GetCommand(), Times.Once);
                    mockCommand.Verify(x => x.IsValid(), Times.Once);
                    request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                    messageBus.Verify(x => x.SendCommand(It.IsAny<CreateChargeReversal>()), Times.Once);
                    var result = response as CreatedResult;
                    Assert.NotNull(result);
                    Assert.Equal(201, result.StatusCode);
                    Assert.False(string.IsNullOrWhiteSpace((string)result.Value));
                }

                [Fact]
                public async Task PostReversalWithoutAdminRightsTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var orderMock = new Mock<ChargeProjection>();
                    orderMock.Object.ClientApplication = new ClientApplicationInfo()
                    {
                        ExternalKey = "XPTO"
                    };
                    var clientMock = new Mock<ClientApplicationProjection>();
                    clientMock.Setup(x => x.Status).Returns(ClientApplicationStatus.Active);
                    clientMock.Object.AggregateKey = "XPTOX";
                    clientMock.Object.ExternalKey = "XPTO";
                    queryBus.Setup(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>()))
                        .Returns(Task.FromResult((IEnumerable<ChargeProjection>)new[] { orderMock.Object }));
                    queryBus.Setup(x => x.Send<SnapshotQuery<ClientApplicationProjection>, ClientApplicationProjection>(
                            It.IsAny<SnapshotQuery<ClientApplicationProjection>>()))
                        .Returns(Task.FromResult(clientMock.Object));
                    var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.RevertCharge>();
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                    var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                    var applicationKey = IdentityGenerator.DefaultApplicationKey();
                    var correlationKey = Guid.NewGuid().ToString("N");
                    var reversalAmount = 50m;
                    var internalOrderKey = IdentityGenerator.NewSequentialIdentity();
                    string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                    var mockCommand = new Mock<CreateChargeReversal>(aggregateKey, correlationKey, applicationKey, sagaProcessKey, reversalAmount) { CallBase = true };
                    request.Setup(x => x.GetCommand()).Returns(mockCommand.Object);
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.PostReversal(internalOrderKey, request.Object);
                    
                    request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                    request.Verify(x => x.GetCommand(), Times.Once);
                    mockCommand.Verify(x => x.IsValid(), Times.Once);
                    messageBus.Verify(x => x.SendCommand(It.IsAny<CreateChargeReversal>()), Times.Once);
                    var result = response as CreatedResult;
                    Assert.NotNull(result);
                    Assert.Equal(201, result.StatusCode);
                    Assert.False(string.IsNullOrWhiteSpace((string)result.Value));
                }

                [Fact]
                public async Task InvalidClientReturnsUnauthorized()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var orderMock = new Mock<ChargeProjection>();
                    orderMock.Object.ClientApplication = new ClientApplicationInfo()
                    {
                        ExternalKey = "XPTOY"
                    };
                    var clientMock = new Mock<ClientApplicationProjection>();
                    clientMock.Object.Status = ClientApplicationStatus.Rejected;
                    clientMock.Object.AggregateKey = "XPTOX";
                    clientMock.Object.ExternalKey = "XPTO";
                    queryBus.Setup(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>()))
                        .Returns(Task.FromResult((IEnumerable<ChargeProjection>)new[] { orderMock.Object }));
                    queryBus.Setup(x => x.Send<SnapshotQuery<ClientApplicationProjection>, ClientApplicationProjection>(
                            It.IsAny<SnapshotQuery<ClientApplicationProjection>>()))
                        .Returns(Task.FromResult(clientMock.Object));
                    var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.RevertCharge>();
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                    var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                    var applicationKey = IdentityGenerator.DefaultApplicationKey();
                    var correlationKey = Guid.NewGuid().ToString("N");
                    var reversalAmount = 50m;
                    var internalOrderKey = IdentityGenerator.NewSequentialIdentity();
                    string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                    var mockCommand = new Mock<CreateChargeReversal>(aggregateKey, correlationKey, applicationKey, sagaProcessKey, reversalAmount) { CallBase = true };
                    request.Setup(x => x.GetCommand()).Returns(mockCommand.Object);
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.PostReversal(internalOrderKey, request.Object);

                    request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                    request.Verify(x => x.GetCommand(), Times.Never);
                    mockCommand.Verify(x => x.IsValid(), Times.Never);
                    messageBus.Verify(x => x.SendCommand(It.IsAny<CreateAcquirerAccountCharge>()), Times.Never);
                    var result = response as UnauthorizedResult;
                    Assert.NotNull(result);
                }

                [Fact]
                public async Task PostReversalInvalidCommandTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var orderMock = new Mock<ChargeProjection>();
                    queryBus.Setup(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>()))
                        .Returns(Task.FromResult((IEnumerable<ChargeProjection>)new[] { orderMock.Object }));
                    var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.RevertCharge>();
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                    var applicationKey = IdentityGenerator.DefaultApplicationKey();
                    var correlationKey = Guid.NewGuid().ToString("N");
                    var reversalAmount = 50m;
                    var internalOrderKey = IdentityGenerator.NewSequentialIdentity();
                    string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                    var mockCommand = new Mock<CreateChargeReversal>(aggregateKey, correlationKey, applicationKey, sagaProcessKey, reversalAmount) { CallBase = true };
                    mockCommand.Setup(x => x.IsValid()).Returns(false);
                    request.Setup(x => x.GetCommand()).Returns(mockCommand.Object);
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.PostReversal(internalOrderKey, request.Object);

                    request.Verify(x => x.GetCommand(), Times.Once);
                    mockCommand.Verify(x => x.IsValid(), Times.Once);
                    request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                    messageBus.Verify(x => x.SendCommand(It.IsAny<CreateChargeReversal>()), Times.Never); 
                    var result = response as BadRequestObjectResult;
                    Assert.NotNull(result);
                }

                [Fact]
                public async Task PostChargeDuplicateExceptionTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var orderMock = new Mock<ChargeProjection>();
                    queryBus.Setup(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>()))
                        .Returns(Task.FromResult((IEnumerable<ChargeProjection>)new[] { orderMock.Object }));
                    var request = new Mock<OrdersApi.Contracts.V1.Charge.Commands.RevertCharge>();
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    var aggregateKey = IdentityGenerator.NewSequentialIdentity();
                    var applicationKey = IdentityGenerator.DefaultApplicationKey();
                    var correlationKey = Guid.NewGuid().ToString("N");
                    var reversalAmount = 50m;
                    var internalOrderKey = IdentityGenerator.NewSequentialIdentity();
                    string sagaProcessKey = IdentityGenerator.NewSequentialIdentity();
                    var mockCommand = new Mock<CreateChargeReversal>(aggregateKey, correlationKey, applicationKey, sagaProcessKey, reversalAmount) { CallBase = true };
                    mockCommand.Setup(x => x.IsValid()).Returns(true);
                    request.Setup(x => x.GetCommand()).Returns(mockCommand.Object);
                    messageBus.Setup(x => x.SendCommand(It.IsAny<CreateChargeReversal>()))
                        .Throws(new DuplicateException() { OrignalAggregateKey = aggregateKey });
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.PostReversal(internalOrderKey, request.Object); 

                    request.Verify(x => x.HasAdmnistratorRights(), Times.Once);
                    messageBus.Verify(x => x.SendCommand(It.IsAny<CreateChargeReversal>()), Times.Once);
                    var result = response as ConflictObjectResult;
                    Assert.NotNull(result);
                    Assert.Equal(409, result.StatusCode);
                    Assert.Equal(aggregateKey, result.Value);
                }
            }

            public class GetTests
            {
                [Fact]
                public async Task GetAllUnauthorizedTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var result = new[]
                    {
                        new ChargeProjection()
                        {
                            Reversals = new List<ReversalProjection>()
                            {
                                new ReversalProjection()
                            }
                        }
                    }.AsEnumerable();
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(result));

                    var request = new Mock<GetChargeReversals>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(false);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(false);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversalList( "XPTO", request.Object);

                    request.Verify(x => x.Specification(), Times.Never);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Never);
                    var okResult = response as UnauthorizedResult;
                    Assert.NotNull(okResult);
                }

                [Fact]
                public async Task GetAllTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var result = new[]
                    {
                        new ChargeProjection()
                        {
                            Reversals = new List<ReversalProjection>()
                            {
                                new ReversalProjection()
                            }
                        }
                    }.AsEnumerable();
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(result));

                    var request = new Mock<GetChargeReversals>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversalList("XPTO", request.Object);

                    request.Verify(x => x.Specification(), Times.Once);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                    var okResult = response as OkObjectResult;
                    Assert.NotNull(okResult);
                    Assert.Equal(200, okResult.StatusCode);
                }

                [Fact]
                public async Task GetByIdTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var projection = new Fixture().Create<ChargeProjection>();
                    projection.Reversals = new  List<ReversalProjection>(){new ReversalProjection(){ ReversalKey = "XPTO"}};
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                        (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                        {
                        projection
                        }));
                    var request = new Mock<GetCharge>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversal("XPTO", request.Object);

                    request.Verify(x => x.Specification(), Times.Once);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                    var okResult = response as OkObjectResult;
                    Assert.NotNull(okResult);
                    Assert.Equal(200, okResult.StatusCode);
                }

                [Fact]
                public async Task GetByIdNotFoundTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var projection = new Fixture().Create<ChargeProjection>();
                    projection.Reversals = new List<ReversalProjection>() { new ReversalProjection() { ReversalKey = "ZZZ" } };
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                        (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                        {
                            projection
                        }));
                    var request = new Mock<GetCharge>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversal("XPTO", request.Object);

                    request.Verify(x => x.Specification(), Times.Once);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                    var result = response as NotFoundResult;
                    Assert.NotNull(result);
                    Assert.Equal(404, result.StatusCode);
                }

            }

            public class GetHistoryTests
            {

                [Fact]
                public async Task GetHistoryByIdTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    var projection = new Fixture().Create<ChargeProjection>();
                    projection.Reversals = new List<ReversalProjection>()
                    {
                        new ReversalProjection()
                        {
                            ReversalKey = "XPTO"
                        }
                    };
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                        (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                        {
                        projection
                        }));
                    var request = new Mock<GetReversalHistory>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversalsStatusHistory("x", "XPTO", request.Object);

                    request.Verify(x => x.Specification(), Times.Once);
                    request.Verify(x => x.SetInternalChargeOrderKey(It.IsAny<string>()), Times.Once);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                    var okResult = response as OkObjectResult;
                    Assert.NotNull(okResult);
                    Assert.Equal(200, okResult.StatusCode);
                }

                [Fact]
                public async Task GetHistoryNotFoundTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                        (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                        {

                        }));
                    var request = new Mock<GetReversalHistory>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversalsStatusHistory("x", "XPTO", request.Object);

                    request.Verify(x => x.Specification(), Times.Once);
                    request.Verify(x => x.SetInternalChargeOrderKey(It.IsAny<string>()), Times.Once);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Once);
                    var result = response as NotFoundResult;
                    Assert.NotNull(result);
                    Assert.Equal(404, result.StatusCode);
                }


                [Fact]
                public async Task GetHistoryBadRequestTest()
                {
                    var messageBus = new Mock<IMessageBus>();
                    var queryBus = new Mock<IQueryBus>();
                    queryBus.Setup(x =>
                        x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(
                            It.IsAny<ISpecification<ChargeProjection>>())).Returns(Task.FromResult(
                        (IEnumerable<ChargeProjection>)new List<ChargeProjection>()
                        {

                        }));
                    var request = new Mock<GetReversalHistory>();
                    request.Setup(x => x.HasGlobalQueryRights()).Returns(true);
                    request.Setup(x => x.HasAdmnistratorRights()).Returns(true);
                    request.Setup(x => x.Specification()).Returns(new DirectSpecification<ChargeProjection>(x => true)).Verifiable();
                    var controller = new ChargesController(messageBus.Object, queryBus.Object);

                    var response = await controller.GetReversalsStatusHistory("x",String.Empty, request.Object);

                    request.Verify(x => x.Specification(), Times.Never);
                    request.Verify(x => x.SetInternalChargeOrderKey(It.IsAny<string>()), Times.Never);
                    queryBus.Verify(x => x.Send<ISpecification<ChargeProjection>, IEnumerable<ChargeProjection>>(It.IsAny<ISpecification<ChargeProjection>>()), Times.Never);
                    var result = response as BadRequestObjectResult;
                    Assert.NotNull(result);
                }
            }
        }

    }
}
