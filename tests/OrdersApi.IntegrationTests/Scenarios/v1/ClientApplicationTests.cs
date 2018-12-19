using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Contracts.V1.ClientApplication.Commands;
using OrdersApi.Contracts.V1.ClientApplication.Views;
using OrdersApi.Contracts.V1.Product.Commands;
using OrdersApi.Contracts.V1.Product.Views;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Model.ClientApplicationAggregate;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.ApplicationTests.Fixtures;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
 
namespace OrdersApi.ApplicationTests.Scenarios.v1
{

    [TestCaseOrderer("OrdersApi.ApplicationTests.PriorityOrderer", "OrdersApi.ApplicationTests")]
    [Collection("2")]
    public class ClientApplicationTests
    {
        private readonly TestContext _context;

        public ClientApplicationTests(TestContext context)
        {
            _context = context;
        }

        [Fact, TestPriority(1)]
        public async Task CreateClientApplicationReturnsCreated()
        {
            try
            {
                var requestBody = JsonConvert.SerializeObject(new Fixture().Create<CreateClientApplication>());

                var postResponse =
                    await _context.Client.PostAsync("v1/client-applications", 
                    new StringContent(requestBody, Encoding.UTF8, "application/json"), CancellationToken.None); 
                postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                TestContextKeys.ClientApplicationInternalKey = await _context.GetIdFromHttpResponse(postResponse);
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationInternalKey));
            }
            catch (Exception e)
            {
               Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(2)]
        public async Task GetClientApplicationReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationInternalKey));
                ClientApplicationView app = null;
                var attempts = 0;
                while (app?.Status != ClientApplicationStatus.Active.ToString() && attempts < 4)
                {
                    app = await GetClientApplication();
                }
                Assert.Equal(ClientApplicationStatus.Active.ToString(), app?.Status);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        private async Task<ClientApplicationView> GetClientApplication()
        {
            Thread.Sleep(3000);
            var response = await _context.Client.GetAsync("v1/client-applications/" + TestContextKeys.ClientApplicationInternalKey, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var app = await response.GetJsonObjectFromHttpResponse<ClientApplicationView>();
            Assert.NotNull(app);
            TestContextKeys.ClientApplicationExternalKey = app.ExternalKey;
            Assert.False(string.IsNullOrEmpty(TestContextKeys.ClientApplicationExternalKey));
            Assert.Equal(TestContextKeys.ClientApplicationInternalKey, app.InternalKey);
            return app;

        }

        [Fact, TestPriority(3)]
        public async Task GetClientApplicationsListReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationInternalKey)); 
                var response = await _context.Client.GetAsync("v1/client-applications/?pageSize=100&pageNumber=1", CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var list = await response.GetJsonObjectFromHttpResponse<PagedResult<ClientApplicationView>>();
                Assert.NotNull(list?.Items);
                Assert.NotEmpty(list?.Items);

            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(4)]
        public async Task CreateClientApplicationProductAccessReturnsCreated()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ProductInternalKey));
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationInternalKey));  
                var access = new CreateProductAccess()
                {
                    CorrelationKey = Guid.NewGuid(),
                    CanCharge = true,
                    CanQuery = true,
                    ProductInternalKey = TestContextKeys.ProductInternalKey
                };
                var requestBody = JsonConvert.SerializeObject(access);
                var postResponse =
                    await _context.Client.PostAsync("v1/client-applications/" + TestContextKeys.ClientApplicationInternalKey + "/products",
                        new StringContent(requestBody, Encoding.UTF8, "application/json"), CancellationToken.None);
                postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                TestContextKeys.ClientApplicationProductAccessKey = await _context.GetIdFromHttpResponse(postResponse);
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationProductAccessKey));
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(5)]
        public async Task GetClientApplicationProductAccessListReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationInternalKey)); 
                Thread.Sleep(2000);  
                var response = await _context.Client.GetAsync("v1/client-applications/" + TestContextKeys.ClientApplicationInternalKey
                    + "/products/?pageSize=100&pageNumber=1", CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var list = await response.GetJsonObjectFromHttpResponse<PagedResult<ProductAccessView>>();
                Assert.NotNull(list?.Items);
                Assert.NotEmpty(list?.Items);

            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

    }
}
