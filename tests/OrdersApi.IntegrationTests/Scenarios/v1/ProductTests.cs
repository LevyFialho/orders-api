using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Contracts.V1.Product.Commands;
using OrdersApi.Contracts.V1.Product.Views;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.ApplicationTests.Fixtures;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace OrdersApi.ApplicationTests.Scenarios.v1
{
    [TestCaseOrderer("OrdersApi.ApplicationTests.PriorityOrderer", "OrdersApi.ApplicationTests")]
    [Collection("1")]
    public class ProductTest
    {
        private readonly TestContext _context;
      

        public ProductTest(TestContext context)
        {
            _context = context;
        }

        [Fact, TestPriority(0)]
        public async Task CreateProductReturnsCreated()
        {
            try
            {
                var requestBody = JsonConvert.SerializeObject(new Fixture().Create<CreateProduct>());

                var postResponse =
                    await _context.Client.PostAsync("v1/products", 
                    new StringContent(requestBody, Encoding.UTF8, "application/json"), CancellationToken.None); 
                postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
                TestContextKeys.ProductInternalKey = await _context.GetIdFromHttpResponse(postResponse);
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ProductInternalKey));
            }
            catch (Exception e)
            {
               Assert.True(false, e.ToString());
            }
        }

        [Fact, TestPriority(1)]
        public async Task GetProductReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ProductInternalKey));
                ProductView product = null;
                var attempts = 0;
                while (product?.Status != ProductStatus.Active.ToString() && attempts < 4)
                {
                    product = await GetProduct();
                }
                Assert.Equal(ProductStatus.Active.ToString(), product?.Status);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        private async Task<ProductView> GetProduct()
        {
            Thread.Sleep(3000);
            var response = await _context.Client.GetAsync("v1/products/" + TestContextKeys.ProductInternalKey, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var product = await response.GetJsonObjectFromHttpResponse<ProductView>();
            Assert.NotNull(product);
            TestContextKeys.ProductExternalKey = product.ExternalKey;
            Assert.False(string.IsNullOrEmpty(TestContextKeys.ProductExternalKey));
            Assert.Equal(TestContextKeys.ProductInternalKey, product.InternalKey);
            return product;
        }

        [Fact, TestPriority(2)]
        public async Task GetProductsListReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ProductInternalKey)); 
                var response = await _context.Client.GetAsync("v1/products/?pageSize=100&pageNumber=1", CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var productsList = await response.GetJsonObjectFromHttpResponse<PagedResult<ProductView>>();
                Assert.NotNull(productsList?.Items);
                Assert.NotEmpty(productsList.Items);

            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }


        [Fact, TestPriority(3)]
        public async Task CreateAcquirerConfigurationReturnsCreated()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ProductInternalKey)); 
                var access = new CreateProductAcquirerConfiguration()
                {
                    CorrelationKey = Guid.NewGuid(),
                    CanCharge = true,
                    AcquirerKey = TestContextKeys.AcquirerKey,
                    AccountKey = Guid.NewGuid().ToString(), 
                };
                var requestBody = JsonConvert.SerializeObject(access);
                var postResponse =
                    await _context.Client.PostAsync("v1/products/" + TestContextKeys.ProductInternalKey + "/acquirer-configurations",
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

        [Fact, TestPriority(4)]
        public async Task GetAcquirerConfigurationListReturnsOk()
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(TestContextKeys.ProductInternalKey));
                Thread.Sleep(2000);
                var response = await _context.Client.GetAsync("v1/products/" + TestContextKeys.ProductInternalKey
                    + "/acquirer-configurations/?pageSize=100&pageNumber=1", CancellationToken.None);
                response.EnsureSuccessStatusCode();
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var list = await response.GetJsonObjectFromHttpResponse<PagedResult<AcquirerConfigurationView>>();
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
