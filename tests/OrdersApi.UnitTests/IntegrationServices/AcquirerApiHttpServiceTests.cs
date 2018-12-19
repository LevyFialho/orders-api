using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Application.Filters;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Charge = OrdersApi.Domain.Model.ChargeAggregate.Charge;

namespace OrdersApi.UnitTests.IntegrationServices
{
    public class AcquirerApiHttpServiceTests
    {
        public class InvalidPaymentMethod : IPaymentMethod
        {
            public PaymentMethod Method { get; set; }
        }

        public class GetErrorMessageFromBadRequestResponseTests
        {
            [Fact]
            public void GetErrorMessageFromBadRequestResponseTest()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var erros = new ErrorResponse()
                {
                    Errors = new Error[1]
                    {
                        new Error()
                        {
                            Message = "Test",
                            Code = 1
                        }
                    }
                };
                var integrationService = new AcquirerApiHttpService(eventStore.Object, httpClient.Object, settings, logger.Object);

                var result = integrationService.GetErrorMessageFromBadRequestResponse(erros);
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Equal("1 - Test", result.First());
            }
        }

        public class BuildGetChargeUrlTests
        {
            [Fact]
            public void BuildGetChargeUrlTest()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var charge = fixture.Create<Charge>();
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var integrationService = new AcquirerApiHttpService(eventStore.Object, httpClient.Object, settings, logger.Object);
                var expected = settings?.ApplicationUri + "/v1/acquirers/" + acquirerAccount?.AcquirerKey + "/charges/?externalKey=" + charge?.AggregateKey;
                var result = integrationService.BuildGetChargeUrl(charge.AggregateKey ,acquirerAccount);
                 
                Assert.Equal(expected, result);
            }
        }

        public class CreatePostChargeTests
        {
            [Fact]
            public void CreatePostChargeTestWithDefaultTypeCode()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var product = fixture.Create<Product>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                product.ExternalKey = settings.ExternalPosRentKey + settings.PosRentKey + "1";
                var acquirerAccount = fixture.Create<AcquirerAccount>();

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var postCharge = integrationService.Object.CreatePostCharge(charge, acquirerAccount, product);
                 
                Assert.Equal(charge.AggregateKey, postCharge.ExternalKey);
                Assert.Equal(charge.OrderDetails.ChargeDate, postCharge.ChargeDate);
                Assert.Equal(charge.OrderDetails.Amount, postCharge.ChargeAmount.Amount);
                Assert.Equal(settings.DefaultCurrencyCode, postCharge.ChargeAmount.Currency);
                Assert.Equal(settings.DefaultChargeTypeCode, postCharge.ChargeType);

            }

            [Fact]
            public void CreatePostChargeForExternalPosRentTest()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var product = fixture.Create<Product>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                product.ExternalKey = settings.ExternalPosRentKey;
                var acquirerAccount = fixture.Create<AcquirerAccount>();

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var postCharge = integrationService.Object.CreatePostCharge(charge, acquirerAccount, product);
                 
                Assert.Equal(charge.AggregateKey, postCharge.ExternalKey);
                Assert.Equal(charge.OrderDetails.ChargeDate, postCharge.ChargeDate);
                Assert.Equal(charge.OrderDetails.Amount, postCharge.ChargeAmount.Amount);
                Assert.Equal(settings.DefaultCurrencyCode, postCharge.ChargeAmount.Currency);
                Assert.Equal(settings.ExternalPosRentChargeTypeCode, postCharge.ChargeType);

            }

            [Fact]
            public void CreatePostChargeForPosRentTest()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var charge = fixture.Create<Charge>();
                var product = fixture.Create<Product>();
                product.ExternalKey = settings.PosRentKey;
                var acquirerAccount = fixture.Create<AcquirerAccount>();

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var postCharge = integrationService.Object.CreatePostCharge(charge, acquirerAccount, product);
                 
                Assert.Equal(charge.AggregateKey, postCharge.ExternalKey);
                Assert.Equal(charge.OrderDetails.ChargeDate, postCharge.ChargeDate);
                Assert.Equal(charge.OrderDetails.Amount, postCharge.ChargeAmount.Amount);
                Assert.Equal(settings.DefaultCurrencyCode, postCharge.ChargeAmount.Currency);
                Assert.Equal(settings.PosRentChargeTypeCode, postCharge.ChargeType);

            }
        }

        public class CreateReversalRequestTests
        {
            [Fact]
            public void CreateReversalRequestTest()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>(); 
                var reversal = fixture.Create<Reversal>(); 
                var logger = new Mock<ILogger<AcquirerApiHttpService>>(); 

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var postCharge = integrationService.Object.CreateReversalRequest(reversal);

                Assert.Equal(reversal.ReversalKey, postCharge.ExternalKey); 
                Assert.Equal(reversal.Amount, postCharge.ChargeAmount.Amount);
                Assert.Equal(settings.DefaultCurrencyCode, postCharge.ChargeAmount.Currency); 

            }
        }

        public class CheckIfChargeOrderWasSentTests
        {
            [Fact]
            public async void IfOrderIsNotAcquirerAccountTypeShouldReturnError()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)new InvalidPaymentMethod()) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var result = await integrationService.Object.CheckIfChargeOrderWasSent(charge.PaymentMethodData.GetData() as AcquirerAccount, charge.AggregateKey);

                Assert.False(result.Success); 
                paymentData.Verify(x => x.GetData(), Times.Once);
            }

            [Fact]
            public async void IfAcquirerApiReturnsNotFoundShouldReturnFalse()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.GetAsync(It.IsAny<string>(), settings.AuthenticationToken, It.IsAny<string>()))
                    .Returns(Task.FromResult(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.NotFound
                    }));

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var result = await integrationService.Object.CheckIfChargeOrderWasSent(charge.PaymentMethodData.GetData() as AcquirerAccount, charge.AggregateKey);

                Assert.True(result.Success);
                Assert.False(result.ReturnedObject);
                paymentData.Verify(x => x.GetData(), Times.Once); 
                httpClient.Verify(x => x.GetAsync(It.IsAny<string>(), settings.AuthenticationToken, It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public async void IfAcquirerApiReturnsChargeOrderShouldReturnTrue()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var charge = fixture.Create<Charge>();
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var acquirerResponse = new List<GetChargeResponse>()
                {
                    new GetChargeResponse()
                    {
                        
                    }
                };
                var mockChargeOrder = new Mock<GetChargeResponse>(); 

                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.GetAsync(It.IsAny<string>(), settings.AuthenticationToken, It.IsAny<string>()))
                    .Returns(Task.FromResult(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonConvert.SerializeObject(acquirerResponse), Encoding.UTF8) 
                    }));
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true }; 

                var result = await integrationService.Object.CheckIfChargeOrderWasSent(charge.PaymentMethodData.GetData() as AcquirerAccount, charge.AggregateKey);

                Assert.True(result.Success);
                Assert.True(result.ReturnedObject);
                paymentData.Verify(x => x.GetData(), Times.Once); 
                httpClient.Verify(x => x.GetAsync(It.IsAny<string>(), settings.AuthenticationToken, It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public async void IfHttpClientThrowsExceptionShouldReturnError()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var charge = fixture.Create<Charge>();
                var exception = new Exception();
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.GetAsync(It.IsAny<string>(), settings.AuthenticationToken, It.IsAny<string>()))
                    .Throws(exception);

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var result = await integrationService.Object.CheckIfChargeOrderWasSent(charge.PaymentMethodData.GetData() as AcquirerAccount, charge.AggregateKey);

                Assert.False(result.Success);
                Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.Equal(exception.ToString(), result.Details.First());
                paymentData.Verify(x => x.GetData(), Times.Once);
                httpClient.Verify(x => x.GetAsync(It.IsAny<string>(), settings.AuthenticationToken, It.IsAny<string>()), Times.Once);
            }
        }

        public class SendChargeReversalTests
        {
            [Fact]
            public async void IfOrderIsNotAcquirerAccountTypeShouldReturnError()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)new InvalidPaymentMethod()) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var result = await integrationService.Object.SendReversalOrder(charge, "xpto");

                Assert.False(result.Success);
                integrationService.Verify(x => x.CreateReversalRequest( It.IsAny<Reversal>()), Times.Never);
                paymentData.Verify(x => x.GetData(), Times.Once);
            }

            [Fact]
            public async void IfAcquirerApiReturnsBadRequestShouldReturnSuccess()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                charge.OrderDetails = new OrderDetails()
                {
                    Amount = 100,
                    ChargeDate = DateTime.UtcNow.Date,
                    ProductInternalKey = "xpto"
                };
                var reversal = new Reversal()
                {
                    Amount = 50,
                    ReversalKey = IdentityGenerator.NewSequentialIdentity(),
                    ReversalDueDate = DateTime.UtcNow
                };
                charge.Reversals = new List<Reversal>(){reversal};
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var postCharge = fixture.Create<CreateReversalRequest>();
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var errorDetails = new List<string>() { "X" };
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent(""),
                    }));
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                integrationService.Setup(x => x.CreateReversalRequest(reversal))
                    .Returns(postCharge);
                integrationService.Setup(x => x.GetErrorMessageFromBadRequestResponse(It.IsAny<ErrorResponse>()))
                    .Returns(errorDetails);

                var result = await integrationService.Object.SendReversalOrder(charge, reversal.ReversalKey);

                Assert.True(result.Success);
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
                paymentData.Verify(x => x.GetData(), Times.Once);
                integrationService.Verify(x => x.GetErrorMessageFromBadRequestResponse(It.IsAny<ErrorResponse>()), Times.Once);
                integrationService.Verify(x => x.CreateReversalRequest(reversal), Times.Once);
                httpClient.Verify(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public async void IfAcquirerApiReturnsCreatedShouldReturnSuccess()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                charge.OrderDetails = new OrderDetails()
                {
                    Amount = 100,
                    ChargeDate = DateTime.UtcNow.Date,
                    ProductInternalKey = "xpto"
                };
                var reversal = new Reversal()
                {
                    Amount = 50,
                    ReversalKey = IdentityGenerator.NewSequentialIdentity(),
                    ReversalDueDate = DateTime.UtcNow
                };
                charge.Reversals = new List<Reversal>() { reversal };
                var logger = new Mock<ILogger<AcquirerApiHttpService>>(); 
                var postCharge = fixture.Create<CreateReversalRequest>(); 
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.Created
                    }));
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                integrationService.Setup(x => x.CreateReversalRequest(reversal))
                    .Returns(postCharge);

                var result = await integrationService.Object.SendReversalOrder(charge, reversal.ReversalKey);

                Assert.True(result.Success);
                Assert.Equal(HttpStatusCode.Created, result.StatusCode);
                paymentData.Verify(x => x.GetData(), Times.Once);
                integrationService.Verify(x => x.CreateReversalRequest(reversal), Times.Once);
                httpClient.Verify(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }


            [Fact]
            public async void IfAcquirerApiThrowsExceptionShouldReturnError()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                charge.OrderDetails = new OrderDetails()
                {
                    Amount = 100,
                    ChargeDate = DateTime.UtcNow.Date,
                    ProductInternalKey = "xpto"
                };
                var reversal = new Reversal()
                {
                    Amount = 50,
                    ReversalKey = IdentityGenerator.NewSequentialIdentity(),
                    ReversalDueDate = DateTime.UtcNow
                };
                charge.Reversals = new List<Reversal>() { reversal };
                var logger = new Mock<ILogger<AcquirerApiHttpService>>(); 
                var exception = new Exception();
                var postCharge = fixture.Create<CreateReversalRequest>(); 
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(exception);
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                integrationService.Setup(x => x.CreateReversalRequest(reversal))
                    .Returns(postCharge);

                var result = await integrationService.Object.SendReversalOrder(charge, reversal.ReversalKey);

                Assert.False(result.Success);
                Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.Equal(exception.ToString(), result.Details.First());
                paymentData.Verify(x => x.GetData(), Times.Once);
                integrationService.Verify(x => x.CreateReversalRequest(reversal), Times.Once);
                httpClient.Verify(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
        }

        public class SendChargeOrderTests
        {
            [Fact]
            public async void IfOrderIsNotAcquirerAccountTypeShouldReturnError()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)new InvalidPaymentMethod()) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;

                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                var result = await integrationService.Object.SendChargeOrder(charge);

                Assert.False(result.Success);
                integrationService.Verify(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), It.IsAny<Product>()), Times.Never);
                paymentData.Verify(x => x.GetData(), Times.Once);
            }

            [Fact]
            public async void IfAcquirerApiReturnsBadRequestShouldReturnSuccess()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var product = fixture.Create<Product>();
                var postCharge = fixture.Create<CreateChargeRequest>();
                eventStore.Setup(x => x.GetByIdAsync<Product>(charge.OrderDetails.ProductInternalKey)).Returns(Task.FromResult(product));
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var errorDetails = new List<string>() { "X" };
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent(""),
                    }));
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                integrationService.Setup(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), product))
                    .Returns(postCharge);
                integrationService.Setup(x => x.GetErrorMessageFromBadRequestResponse(It.IsAny<ErrorResponse>()))
                    .Returns(errorDetails);

                var result = await integrationService.Object.SendChargeOrder(charge);

                Assert.True(result.Success);
                Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
                paymentData.Verify(x => x.GetData(), Times.Once);
                integrationService.Verify(x => x.GetErrorMessageFromBadRequestResponse(It.IsAny<ErrorResponse>()), Times.Once);
                integrationService.Verify(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), product), Times.Once);
                httpClient.Verify(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public async void IfAcquirerApiReturnsCreatedShouldReturnSuccess()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var product = fixture.Create<Product>();
                var postCharge = fixture.Create<CreateChargeRequest>();
                eventStore.Setup(x => x.GetByIdAsync<Product>(charge.OrderDetails.ProductInternalKey)).Returns(Task.FromResult(product));
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.Created
                    }));
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                integrationService.Setup(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), product))
                    .Returns(postCharge);

                var result = await integrationService.Object.SendChargeOrder(charge);

                Assert.True(result.Success);
                Assert.Equal(HttpStatusCode.Created, result.StatusCode);
                paymentData.Verify(x => x.GetData(), Times.Once);
                integrationService.Verify(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), product), Times.Once);
                httpClient.Verify(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }


            [Fact]
            public async void IfAcquirerApiThrowsExceptionShouldReturnError()
            {
                var fixture = new Fixture();
                var eventStore = new Mock<AggregateDataSource>(null, null, null);
                var httpClient = new Mock<IHttpClient>();
                var settings = fixture.Create<AcquirerApiSettings>();
                var charge = fixture.Create<Charge>();
                var logger = new Mock<ILogger<AcquirerApiHttpService>>();
                var product = fixture.Create<Product>();
                var exception = new Exception();
                var postCharge = fixture.Create<CreateChargeRequest>();
                eventStore.Setup(x => x.GetByIdAsync<Product>(charge.OrderDetails.ProductInternalKey)).Returns(Task.FromResult(product));
                var acquirerAccount = fixture.Create<AcquirerAccount>();
                var paymentData = new Mock<PaymentMethodData>((IPaymentMethod)acquirerAccount) { CallBase = true };
                charge.PaymentMethodData = paymentData.Object;
                httpClient.Setup(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(exception);
                var integrationService = new Mock<AcquirerApiHttpService>(eventStore.Object, httpClient.Object, settings, logger.Object) { CallBase = true };
                integrationService.Setup(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), product))
                    .Returns(postCharge);

                var result = await integrationService.Object.SendChargeOrder(charge);

                Assert.False(result.Success);
                Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
                Assert.Equal(exception.ToString(), result.Details.First());
                paymentData.Verify(x => x.GetData(), Times.Once);
                integrationService.Verify(x => x.CreatePostCharge(charge, It.IsAny<AcquirerAccount>(), product), Times.Once);
                httpClient.Verify(x => x.PostAsync(It.IsAny<string>(), postCharge, settings.AuthenticationToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
        }


    }
}

