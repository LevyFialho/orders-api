using System;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Models;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Commands
{
    public class CommandTests
    { 
        [Fact]
        public void ConstructorTest()
        {
            var correlationId = Guid.NewGuid().ToString();
            var aggregateId = IdentityGenerator.NewSequentialIdentity();
            var applicationKey = Guid.NewGuid().ToString();
            var sagaProcessKey = IdentityGenerator.NewSequentialIdentity(); 
            var command = new Mock<Command>(aggregateId, correlationId, applicationKey, sagaProcessKey) {CallBase = true};
            Assert.Equal(correlationId, command.Object.CorrelationKey);
            Assert.Equal(aggregateId, command.Object.AggregateKey);
            Assert.Equal(sagaProcessKey, command.Object.SagaProcessKey);
            Assert.Equal(applicationKey, command.Object.ApplicationKey);
        }
    }
}
