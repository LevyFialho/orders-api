using System;
using System.Collections.Generic;
using System.Threading; 
using AutoFixture;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events; 
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries; 
using OrdersApi.Domain.Commands.Charge.Reversal;
using OrdersApi.Domain.EventHandlers;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate; 
using OrdersApi.Domain.Model.Projections.ChargeProjections; 
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.EventHandlers
{
    public class ReversalEventHandlerTests
    {
        public static IntegrationSettings IntegrationSettings = new IntegrationSettings()
        {
            ProcessingRetryLimit = 60,
            SettlementVerificationInterval = 60,
            SettlementVerificationLimit = 5000,
            ProcessingRetryInterval = 5
        };

        public class HandleCreatedEventTests
        {

            [Fact]
            public async void HandleNewReversal()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var eventData = fixture.Create<ReversalCreated>();
                var charge = new Mock<ChargeProjection>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(charge.Object);
                var bus = new Mock<ICommandScheduler>();
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(charge.Object);

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ReversalEventsHandler>(repository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object,IntegrationSettings)
                {
                    CallBase = true
                };

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once); 
                charge.Verify(x => x.Update(eventData), Times.Once);
                handler.Verify(x => x.ScheduleReversalProcessing(It.IsAny<ChargeProjection>(), It.IsAny<IEvent>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);

            }

        }

        public class ShouldRetryTests
        {
            [Fact]
            public void ShouldNotRetryIfErrorPast60Minutes()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.Error,
                    History = new List<ChargeStatusProjection>()
                    {
                        new ChargeStatusProjection()
                        {
                            Status = ChargeStatus.Error,
                            Date = DateTime.UtcNow.AddMinutes(-61)
                        }
                    },
                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldRetry(projection, "XPTO"));
            }

            [Fact]
            public void ShouldNotRetryIfStatusIsSettled()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.Settled,

                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);



                Assert.False(handler.ShouldRetry(projection, "XPTO"));
            }

            [Fact]
            public void ShouldNotRetryIfStatusIsProcessed()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.Processed
                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldRetry(projection, "XPTO"));
            }

            [Fact]
            public void ShouldRetry()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.Rejected,
                    History = new List<ChargeStatusProjection>()
                    {
                        new ChargeStatusProjection()
                        {
                            Status = ChargeStatus.Rejected,
                            Date = DateTime.UtcNow.AddMinutes(-30)
                        }
                    },
                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);

                Assert.True(handler.ShouldRetry(projection, "XPTO"));
            }
        }

        public class ShouldVerifySettlementTests
        {
            [Fact]
            public void ShouldNotVerifySettlementIfErrorPastLimit()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.Processed,
                    History = new List<ChargeStatusProjection>() {
                    new ChargeStatusProjection()
                    {
                        Status = ChargeStatus.NotSettled,
                        Date = DateTime.UtcNow.AddMinutes(-5001)
                    }}

                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);



                Assert.False(handler.ShouldVerifySettlement(projection, "XPTO"));
            }



            [Fact]
            public void ShouldNotVerifyIfStatusIsSettled()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.Settled
                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldVerifySettlement(projection, "XPTO"));
            }

            [Fact]
            public void ShouldVerify()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var reversal = new ReversalProjection
                {
                    Amount = 10,
                    ReversalKey = "XPTO",
                    ReversalDueDate = DateTime.UtcNow.AddDays(1),
                    Status = ChargeStatus.NotSettled,
                    History = new List<ChargeStatusProjection>()
                    {
                        new ChargeStatusProjection()
                        {
                            Status = ChargeStatus.NotSettled,
                            Date = DateTime.UtcNow.AddMinutes(-30)
                        }
                    }
                };
                projection.Reversals = new List<ReversalProjection>() { reversal };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object,IntegrationSettings);

                Assert.True(handler.ShouldVerifySettlement(projection, "XPTO"));
            }
        }

        public class ScheduleReversalProcessingTests
        {
            [Fact]
            public async void ShouldSendChargeToAcquirer()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var @event = fixture.Create<ChargeCreated>();
                var timeSpan = fixture.Create<TimeSpan>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>(); 
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object, 
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                projection.Method = PaymentMethod.AcquirerAccount;
                await handler.ScheduleReversalProcessing(projection, @event, "XPTO",timeSpan);

                bus.Verify(x => x.RunDelayed(timeSpan, It.IsAny<ProcessAcquirerAccountReversal>()), Times.Once);
            }
        }

        public class ScheduleSettlementVerification
        {
            [Fact]
            public async void ShouldScheduleTest()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var @event = fixture.Create<ChargeCreated>();
                var timeSpan = fixture.Create<TimeSpan>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>(); 
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ReversalEventsHandler(repository.Object, 
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);

                projection.Method = PaymentMethod.AcquirerAccount;
                await handler.ScheduleSettlementVerification(projection, @event, "XPTO", timeSpan);

                bus.Verify(x => x.RunDelayed(timeSpan, It.IsAny<VerifyReversalSettlement>()), Times.Once);
            }
        }
     
        public class HandleProcessedEventTests
        {
            [Fact]
            public async void HandleExistingCharge()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<AcquirerAccountReversalProcessed>();
                eventData.Result = new IntegrationResult(Result.Sucess);
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object); 
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ReversalEventsHandler>(repository.Object, 
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                handler.Verify(x => x.ScheduleSettlementVerification(It.IsAny<ChargeProjection>(), It.IsAny<IEvent>(), It.IsAny<string>(),It.IsAny<TimeSpan>()), Times.Never);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }
        }

        public class HandleSettledEventTests
        {
            [Fact]
            public async void HandleExistingCharge()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ReversalSettled>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object); 
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new ReversalEventsHandler(repository.Object, 
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                await handler.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }

        }

        public class HandleAcquirerAccountReversalErrorEventTests
        {
            [Fact]
            public async void HandleExistingChargeWithRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<AcquirerAccountReversalError>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object); 
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
               

                var handler = new Mock<ReversalEventsHandler>(repository.Object, 
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldRetry(mockProjection.Object, eventData.ReversalKey)).Returns(true);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldRetry(mockProjection.Object, eventData.ReversalKey), Times.Once);
                handler.Verify(x => x.ScheduleReversalProcessing(mockProjection.Object, eventData, It.IsAny<string>(),It.IsAny<TimeSpan>()), Times.Once); 
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }

            [Fact]
            public async void HandleExistingChargeWithNoRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<AcquirerAccountReversalError>();

                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                 
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
              

                var handler = new Mock<ReversalEventsHandler>(repository.Object, 
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldRetry(mockProjection.Object, eventData.ReversalKey)).Returns(false);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldRetry(mockProjection.Object, eventData.ReversalKey), Times.Once);
                handler.Verify(x => x.ScheduleReversalProcessing(mockProjection.Object, eventData, It.IsAny<string>(),It.IsAny<TimeSpan>()), Times.Never); 
                mockProjection.Verify(x => x.Update(eventData), Times.Once);
            }
        }

        public class HandleReversalNotSettledEventTests
        {
            [Fact]
            public async void HandleExistingChargeWithRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ReversalNotSettled>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object); 
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ReversalEventsHandler>(repository.Object, 
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldVerifySettlement(mockProjection.Object, eventData.ReversalKey)).Returns(true);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldVerifySettlement(mockProjection.Object, eventData.ReversalKey), Times.Once);
                handler.Verify(x => x.ScheduleSettlementVerification(mockProjection.Object, eventData, eventData.ReversalKey, It.IsAny<TimeSpan>()), Times.Once);
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }

            [Fact]
            public async void HandleExistingChargeWithNoRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ReversalNotSettled>();

                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                 
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ReversalEventsHandler>(repository.Object, 
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldVerifySettlement(mockProjection.Object, eventData.ReversalKey)).Returns(false);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldVerifySettlement(mockProjection.Object, eventData.ReversalKey), Times.Once);
                handler.Verify(x => x.ScheduleSettlementVerification(mockProjection.Object, eventData, eventData.ReversalKey, It.IsAny<TimeSpan>()), Times.Never);
                mockProjection.Verify(x => x.Update(eventData), Times.Once);
            }
        }
    }
}
