using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Contracts.V1.Charge.Commands;
using OrdersApi.Contracts.V1.Charge.Views;
using OrdersApi.Contracts.V1.ClientApplication.Commands;
using OrdersApi.Contracts.V1.ClientApplication.Views;
using OrdersApi.Contracts.V1.Product.Commands;
using OrdersApi.Contracts.V1.Product.Views;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.ApplicationTests.Fixtures;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace OrdersApi.ApplicationTests.Scenarios.v1
{

    [TestCaseOrderer("OrdersApi.ApplicationTests.PriorityOrderer", "OrdersApi.ApplicationTests")]
    [Collection("3")]
    public class AcquirerAccountChargesTests
    {
        private readonly TestContext _context;

        public AcquirerAccountChargesTests(TestContext context)
        {
            _context = context;
        }

        [Fact, TestPriority(1)]
        public async Task CreateAcquirerAccountChargeReturnsCreated()
        {
            try
            {
                var charge = new CreateAcquirerAccountCharge()
                {
                    ProductExternalKey = TestContextKeys.ProductExternalKey,
                    Amount = 100,
                    ChargeDate = DateTime.Today.AddDays(1),
                    CorrelationKey = Guid.NewGuid(),
                    Payment = new AcquirerAccountDetails()
                    {
                        MerchantKey = "TEST",
                        AcquirerKey = TestContextKeys.AcquirerKey, 
                    }
                };
                var requestBody = JsonConvert.SerializeObject(charge);

                var postResponse =
                    await _context.Client.PostAsync("v1/charges/acquirer-account",
                    new StringContent(requestBody, Encoding.UTF8, "application/json"), CancellationToken.None);

                postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                TestContextKeys.AcquirerChargeInternalKey = await _context.GetIdFromHttpResponse(postResponse);
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.AcquirerChargeInternalKey));
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(2)]
        public async Task GetAcquirerAccountChargeReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.AcquirerChargeInternalKey));
                AcquirerAccountChargeView charge = null;
                var attempts = 0;
                while (charge?.Status != ChargeStatus.Processed.ToString() && attempts < 9)
                {
                    charge = await GetCharge();
                }
                Assert.Equal(ChargeStatus.Processed.ToString(), charge?.Status);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        private async Task<AcquirerAccountChargeView> GetCharge()
        {
            Thread.Sleep(3000);
            var response = await _context.Client.GetAsync("v1/charges/" + TestContextKeys.AcquirerChargeInternalKey, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var charge = await response.GetJsonObjectFromHttpResponse<AcquirerAccountChargeView>();
            Assert.NotNull(charge);
            Assert.Equal(100, charge.Amount);
            return charge;
        }
        
        [Fact, TestPriority(3)]
        public async Task CreateAcquirerAccountChargeReversalReturnsCreated()
        {
            try
            {
                var charge = new RevertCharge()
                { 
                    Amount = 50, 
                    CorrelationKey = Guid.NewGuid(), 
                };
                var requestBody = JsonConvert.SerializeObject(charge);

                var postResponse =
                    await _context.Client.PostAsync("v1/charges/"+ TestContextKeys.AcquirerChargeInternalKey + "/reversals",
                        new StringContent(requestBody, Encoding.UTF8, "application/json"), CancellationToken.None);

                postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                TestContextKeys.AcquirerChargeReversalInternalKey = await _context.GetIdFromHttpResponse(postResponse);
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.AcquirerChargeReversalInternalKey));
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(4)]
        public async Task GetAcquirerAccountChargeReversalReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.AcquirerChargeReversalInternalKey));
                ChargeReversalView charge = null;
                var attempts = 0;
                while (charge?.Status != ChargeStatus.Processed.ToString() && attempts < 9)
                {
                    charge = await GetReversal();
                }
                Assert.Equal(ChargeStatus.Processed.ToString(), charge?.Status);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        private async Task<ChargeReversalView> GetReversal()
        {
            Thread.Sleep(3000);
            var response = await _context.Client.GetAsync("v1/charges/" + TestContextKeys.AcquirerChargeInternalKey + "/reversals/" + TestContextKeys.AcquirerChargeReversalInternalKey, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var charge = await response.GetJsonObjectFromHttpResponse<ChargeReversalView>();
            Assert.NotNull(charge);
            Assert.Equal(50, charge.Amount);
            return charge;
        }

        [Fact, TestPriority(5)]
        public async Task<PagedResult<ChargeReversalView>> GetReversalList()
        { 
            var response = await _context.Client.GetAsync("v1/charges/" + TestContextKeys.AcquirerChargeInternalKey + "/reversals?pageSize=100&pageNumber=1", CancellationToken.None);
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await response.GetJsonObjectFromHttpResponse<PagedResult<ChargeReversalView>>();
            Assert.NotNull(list?.Items);
            Assert.NotEmpty(list?.Items);
            return list;
        }

        [Fact, TestPriority(5)]
        public async Task GetAcquirerAccountReversalStatusHistoryReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.AcquirerChargeInternalKey));
                var response = await _context.Client.GetAsync("v1/charges/"
                                                              + TestContextKeys.AcquirerChargeInternalKey + "/reversals/"+
                                                              TestContextKeys.AcquirerChargeReversalInternalKey+ "/history/?pageSize=10&pageNumber=1", CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var history = await response.GetJsonObjectFromHttpResponse<PagedResult<ChargeStatusView>>();
                Assert.NotNull(history?.Items);
                Assert.NotEmpty(history.Items);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(3)]
        public async Task GetAcquirerAccountChargeStatusHistoryReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.AcquirerChargeInternalKey)); 
                var response = await _context.Client.GetAsync("v1/charges/"
                    + TestContextKeys.AcquirerChargeInternalKey + "/history/?pageSize=10&pageNumber=1", CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var history = await response.GetJsonObjectFromHttpResponse<PagedResult<ChargeStatusView>>();
                Assert.NotNull(history?.Items);
                Assert.NotEmpty(history.Items);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(3)]
        public async Task GetAcquirerAccountChargeListReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ClientApplicationInternalKey));
                var response = await _context.Client.GetAsync("v1/charges/?pageSize=100&pageNumber=1&CreatedDate.From=" + DateTime.Today.ToString("u"), CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var list = await response.GetJsonObjectFromHttpResponse<SeekResult<AcquirerAccountChargeView>>();
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
