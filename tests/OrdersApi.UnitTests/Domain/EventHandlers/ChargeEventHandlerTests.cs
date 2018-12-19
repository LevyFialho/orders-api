using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Exceptions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Commands.Charge;
using OrdersApi.Domain.EventHandlers;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Projections;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.UnitTests.Cqrs.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq; 
using Xunit;

namespace OrdersApi.UnitTests.Domain.EventHandlers
{
    public class ChargeEventHandlerTests
    {
        public static IntegrationSettings IntegrationSettings = new IntegrationSettings()
        {
            ProcessingRetryLimit = 60,
            SettlementVerificationInterval = 60,
            SettlementVerificationLimit = 5000,
            ProcessingRetryInterval = 5
        };

        public class GetSettlementScheduleTests
        {
            [Fact]
            public void GetSettlementScheduleTest()
            {
                var projection = new Fixture().Create<ChargeProjection>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>(); 
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>(); 
                var bus = new Mock<ICommandScheduler>(); 
                var integrationSettings = new IntegrationSettings()
                {
                    SettlementVerificationInterval = 5
                };
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>(); 

                var tracerMockDictionary = new Dictionary<string, object>();
                  

                var handler = new ChargeEventsHandler(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object,  integrationSettings);
                var result = handler.GetSettlementSchedule(projection);
                Assert.True(result > TimeSpan.Zero);
            }

            [Fact]
            public void GetSettlementScheduleInvalidSettingTest()
            {
                var projection = new Fixture().Create<ChargeProjection>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var integrationSettings = new IntegrationSettings()
                {
                    SettlementVerificationInterval = -5
                };
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();  

                var handler = new ChargeEventsHandler(repository.Object,
                    productRepository.Object,
                    clientAppRepository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object,  integrationSettings);
                var result = handler.GetSettlementSchedule(projection);
                Assert.Equal(TimeSpan.Zero, result);
            }

            [Fact]
            public void GetSettlementTimeSpanOldOrderDateTest()
            {
                var projection = new Fixture().Create<ChargeProjection>();
                projection.OrderDetails.ChargeDate = DateTime.Today.AddYears(-1);
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>(); 
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
            

                var handler = new ChargeEventsHandler(repository.Object,
                    productRepository.Object,
                    clientAppRepository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object,  IntegrationSettings);
                var result = handler.GetSettlementSchedule(projection);
                Assert.True(result > TimeSpan.Zero);
            }
        }

        public class HandleCreatedEventTests
        {  

            [Fact]
            public async void HandleExistingAggregate()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var product = fixture.Create<ProductProjection>();
                var eventData = fixture.Create<ChargeCreated>();
                productRepository.Setup(x => x.Get(eventData.AggregateKey)).Returns(product);
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var clientApp = fixture.Create<ClientApplicationProjection>();
                clientAppRepository.Setup(x => x.Get(eventData.ApplicationKey)).Returns(clientApp);
                var bus = new Mock<ICommandScheduler>();
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(new ChargeProjection());

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                productRepository.Verify(x => x.Get(eventData.OrderDetails.ProductInternalKey), Times.Once);
                clientAppRepository.Verify(x => x.Get(eventData.ApplicationKey), Times.Once);
                repository.Verify(x => x.GetFiltered(It.IsAny<ISpecification<ChargeProjection>>()), Times.Never);
                repository.Verify(x => x.AddAynsc(It.IsAny<ChargeProjection>()), Times.Never);
                handler.Verify(x => x.ScheduleChargeProcessing(It.IsAny<ChargeProjection>(), It.IsAny<IEvent>(), It.IsAny<TimeSpan>()), Times.Never);
                
            }

            [Fact]
            public async void HandleNewCharge()
            {
                var fixture = new Fixture();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var product = fixture.Create<ProductProjection>();
                var eventData = fixture.Create<ChargeCreated>();
                var paymentData = fixture.Create<AcquirerAccount>();
                eventData.PaymentMethodData = new PaymentMethodData(paymentData);
                productRepository.Setup(x => x.Get(eventData.AggregateKey)).Returns(product);
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var clientApp = fixture.Create<ClientApplicationProjection>();
                clientAppRepository.Setup(x => x.Get(eventData.ApplicationKey)).Returns(clientApp);
                var bus = new Mock<ICommandScheduler>(); 
                //Setup existing aggregate
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(default(ChargeProjection));

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();  

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                productRepository.Verify(x => x.Get(eventData.OrderDetails.ProductInternalKey), Times.Once);
                clientAppRepository.Verify(x => x.Get(eventData.ApplicationKey), Times.Once);
                repository.Verify(x => x.AddAynsc(It.IsAny<ChargeProjection>()), Times.Once);
                handler.Verify(x => x.ScheduleChargeProcessing(It.IsAny<ChargeProjection>(), It.IsAny<IEvent>(), It.IsAny<TimeSpan>()), Times.Once);
            }
        }

        public class ShouldRetryTests
        {
            [Fact]
            public void ShouldNotRetryIfErrorPast60Minutes()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Rejected;
                projection.History = new List<ChargeStatusProjection>()
                {
                    new ChargeStatusProjection()
                    {
                        Status = ChargeStatus.Rejected,
                        Date = DateTime.UtcNow.AddMinutes(-61)
                    }
                };
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object, 
                                                      productRepository.Object, 
                                                      clientAppRepository.Object, 
                                                      bus.Object, 
                                                      chargeProjectionLogger.Object,IntegrationSettings);

                Assert.False(handler.ShouldRetry(projection));
            }

            [Fact]
            public void ShouldNotRetryIfStatusIsRejected()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Rejected; 
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldRetry(projection));
            }

            [Fact]
            public void ShouldNotRetryIfStatusIsProcessed()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Processed;
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldRetry(projection));
            }

            [Fact]
            public void ShouldRetry()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Error;
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                projection.History = new List<ChargeStatusProjection>()
                {
                    new ChargeStatusProjection()
                    {
                        Status = ChargeStatus.Error,
                        Date = DateTime.UtcNow.AddMinutes(-30)
                    }
                };
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.True(handler.ShouldRetry(projection));
            }
        }

        public class ShouldVerifySettlementTests
        {
            [Fact]
            public void ShouldNotVerifySettlementIfErrorPastLimit()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Rejected;
                projection.History = new List<ChargeStatusProjection>()
                {
                    new ChargeStatusProjection()
                    {
                        Status = ChargeStatus.NotSettled,
                        Date = DateTime.UtcNow.AddMinutes(-5001)
                    }
                };
                
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldVerifySettlement(projection));
            }

            [Fact]
            public void ShouldNotVerifyIfStatusIsExpired()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Error;
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldVerifySettlement(projection));
            }

            [Fact]
            public void ShouldNotVerifyIfStatusIsSettled()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.Settled;
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.False(handler.ShouldVerifySettlement(projection));
            }

            [Fact]
            public void ShouldVerify()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                projection.Status = ChargeStatus.NotSettled;
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                projection.History = new List<ChargeStatusProjection>()
                {
                    new ChargeStatusProjection()
                    {
                        Status = ChargeStatus.NotSettled,
                        Date = DateTime.UtcNow.AddMinutes(-30)
                    }
                };
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                Assert.True(handler.ShouldVerifySettlement(projection));
            }
        }

        public class ScheduleChargeProcessingTests
        {
            [Fact]
            public async void ShouldSendChargeToAcquirer()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var @event = fixture.Create<ChargeCreated>();
                var timeSpan = fixture.Create<TimeSpan>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                projection.Method = PaymentMethod.AcquirerAccount;
                await handler.ScheduleChargeProcessing(projection, @event, timeSpan);

                bus.Verify(x => x.RunDelayed(timeSpan, It.IsAny<SendChargeToAcquirer>()), Times.Once);
            } 
        }

        public class ScheduleAcquirerSettlementVerificationTests
        {
            [Fact]
            public async void ShouldScheduleTest()
            {
                var fixture = new Fixture();
                var projection = fixture.Create<ChargeProjection>();
                var @event = fixture.Create<ChargeCreated>();
                var timeSpan = fixture.Create<TimeSpan>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();
                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                var handler = new ChargeEventsHandler(repository.Object,
                    productRepository.Object,
                    clientAppRepository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings);

                projection.Method = PaymentMethod.AcquirerAccount;
                await handler.ScheduleSettlementVerification(projection, @event, timeSpan);

                bus.Verify(x => x.RunDelayed(timeSpan, It.IsAny<VerifyAcquirerSettlement>()), Times.Once);
            }
        }

        public class HandleExpiredEventTests
        {
            [Fact]
            public async void HandleExistingCharge()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>(); 
                var eventData = fixture.Create<ChargeExpired>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                await handler.Handle(eventData, CancellationToken.None);

                mockProjection.Verify(x => x.Update(eventData), Times.Once);
                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once); 

            } 
        }

        public class HandleProcessedEventTests
        {
            [Fact]
            public async void HandleExistingCharge()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ChargeProcessed>();
                eventData.Result = new IntegrationResult(Result.Sucess);
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                    productRepository.Object,
                    clientAppRepository.Object,
                    bus.Object,
                    chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                handler.Verify(x => x.ScheduleSettlementVerification(It.IsAny<ChargeProjection>(), It.IsAny<IEvent>(), It.IsAny<TimeSpan>()), Times.Never);
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
                var eventData = fixture.Create<ChargeSettled>(); 
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new ChargeEventsHandler(repository.Object,
                                                      productRepository.Object,
                                                      clientAppRepository.Object,
                                                      bus.Object,
                                                      chargeProjectionLogger.Object, IntegrationSettings);

                await handler.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }

        }

        public class HandleChargeCouldNotBeProcessedEventTests
        {
            [Fact]
            public async void HandleExistingChargeWithRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ChargeCouldNotBeProcessed>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldRetry(mockProjection.Object)).Returns(true);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldRetry(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ScheduleChargeProcessing(mockProjection.Object, eventData, It.IsAny<TimeSpan>()), Times.Once);
                bus.Verify(x => x.RunNow(It.IsAny<ExpireCharge>()), Times.Never());
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }

            [Fact]
            public async void HandleExistingChargeWithNoRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ChargeCouldNotBeProcessed>();

                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);

                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();
                

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldRetry(mockProjection.Object)).Returns(false);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldRetry(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ScheduleChargeProcessing(mockProjection.Object, eventData, It.IsAny<TimeSpan>()), Times.Never);
                bus.Verify(x => x.RunNow(It.IsAny<ExpireCharge>()), Times.Once());
                mockProjection.Verify(x => x.Update(eventData), Times.Once);
            }
        }

        public class HandleChargeNotSettledEventTests
        {
            [Fact]
            public async void HandleExistingChargeWithRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ChargeNotSettled>();
                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);
                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldVerifySettlement(mockProjection.Object)).Returns(true);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldVerifySettlement(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ScheduleSettlementVerification(mockProjection.Object, eventData, It.IsAny<TimeSpan>()), Times.Once);
                mockProjection.Verify(x => x.Update(eventData), Times.Once);

            }

            [Fact]
            public async void HandleExistingChargeWithNoRetry()
            {
                var fixture = new Fixture();
                var mockProjection = new Mock<ChargeProjection>();
                var eventData = fixture.Create<ChargeNotSettled>();

                var repository = new Mock<IQueryableRepository<ChargeProjection>>();
                repository.Setup(x => x.Get(eventData.AggregateKey)).Returns(mockProjection.Object);

                var productRepository = new Mock<IQueryableRepository<ProductProjection>>();
                var clientAppRepository = new Mock<IQueryableRepository<ClientApplicationProjection>>();
                var bus = new Mock<ICommandScheduler>();

                var chargeProjectionLogger = new Mock<ILogger<ChargeProjection>>();

                var handler = new Mock<ChargeEventsHandler>(repository.Object,
                                                            productRepository.Object,
                                                            clientAppRepository.Object,
                                                            bus.Object,
                                                            chargeProjectionLogger.Object, IntegrationSettings)
                {
                    CallBase = true
                };

                handler.Setup(x => x.ShouldVerifySettlement(mockProjection.Object)).Returns(false);

                await handler.Object.Handle(eventData, CancellationToken.None);

                repository.Verify(x => x.Get(eventData.AggregateKey), Times.Once);
                repository.Verify(x => x.UpdateAsync(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ShouldVerifySettlement(mockProjection.Object), Times.Once);
                handler.Verify(x => x.ScheduleSettlementVerification(mockProjection.Object, eventData, It.IsAny<TimeSpan>()), Times.Never); 
                mockProjection.Verify(x => x.Update(eventData), Times.Once);
            }
        }

       
    }
}
