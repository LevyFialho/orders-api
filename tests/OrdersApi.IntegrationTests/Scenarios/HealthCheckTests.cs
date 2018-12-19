using System;
using System.Net;
using System.Threading.Tasks;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.ApplicationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace OrdersApi.ApplicationTests.Scenarios
{
    [Collection("0")]
    public class HealthCheckTests
    {
        private readonly TestContext _context;

        public HealthCheckTests(TestContext context)
        {
            _context = context;
        }

        [Fact]
        public async Task PingReturnsOkResponse()
        {
            try
            {
                var response = await _context.Client.GetAsync("/management/ping");

                response.EnsureSuccessStatusCode();

                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

        [Fact]
        public async Task AppInfoReturnsOkResponse()
        {
            try
            {
                var response = await _context.Client.GetAsync("/management/app-info");

                response.EnsureSuccessStatusCode();

                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
               Assert.True(false, e.ToString());
            }
        }

        [Fact]
        public async Task HealthcheckReturnsOkResponse()
        {
            try
            {
                var response = await _context.Client.GetAsync("/management/health-check");
                var data = await response.GetStringromHttpResponse();
                response.EnsureSuccessStatusCode();

                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                Assert.True(false, e.ToString());
            }
        }

    }
}
