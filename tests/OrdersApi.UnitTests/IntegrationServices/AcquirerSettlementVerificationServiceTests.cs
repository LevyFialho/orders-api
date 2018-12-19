using AutoFixture;
using AutoFixture.AutoMoq;
using OrdersApi.Cqrs.Commands;
using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Queries;
using OrdersApi.Cqrs.Queries.Specifications;
using OrdersApi.Domain.Model.Projections.ChargeProjections;
using OrdersApi.Infrastructure.MessageBus.Abstractions;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OrdersApi.UnitTests.IntegrationServices
{
    public class AcquirerSettlementVerificationServiceTests
    {
        public class VerifyCharges
        {
            [Fact]
            public async void ShouldVerifyChargesSettlements()
            {
                var chargeDate = new DateTime(2018, 11, 08);

                var charges = new List<ChargeProjection>()
                {
                    new ChargeProjection()
                    {
                        Status = OrdersApi.Domain.Model.ChargeAggregate.ChargeStatus.Processed,

                        OrderDetails = new OrdersApi.Domain.Model.ChargeAggregate.OrderDetails()
                        {
                            ChargeDate = chargeDate
                        }
                    },
                    new ChargeProjection()
                    {
                        Status = OrdersApi.Domain.Model.ChargeAggregate.ChargeStatus.Processed,

                        OrderDetails = new OrdersApi.Domain.Model.ChargeAggregate.OrderDetails()
                        {
                            ChargeDate = chargeDate
                        }
                    }

                }.AsEnumerable();

                var fixture = new Fixture().Customize(new AutoMoqCustomization());

                var repository = fixture.Freeze<Mock<IQueryableRepository<ChargeProjection>>>();

                Func<ISpecification<ChargeProjection>, Expression<Func<ChargeProjection, object>>, int?, Task<IEnumerable<ChargeProjection>>> getFilteredSortByAsyncMock = (specification, sort, limit) =>
                {
                    var filteredCharges = charges
                                            .Where(specification.SatisfiedBy().Compile())
                                                .OrderBy(sort.Compile())
                                                    .AsEnumerable();

                    return Task.FromResult(filteredCharges);
                };

                repository.Setup(bus => bus.GetFilteredSortByAsync(It.IsAny<ISpecification<ChargeProjection>>(),
                                                                   It.IsAny<Expression<Func<ChargeProjection, object>>>(),
                                                                   It.IsAny<int?>())).Returns(getFilteredSortByAsyncMock);

                var service = fixture.Create<Mock<AcquirerSettlementVerificationService>>();

                service.Setup(x => x.SendVerifySettlementCommandForCharge(It.IsAny<ChargeProjection>())).Returns(Task.CompletedTask);

                await service.Object.VerifyCharges();

                // Assertions
                service.Verify(x => x.SendVerifySettlementCommandForCharge(It.IsAny<ChargeProjection>()), Times.Exactly(charges.Count()));
            }
        }

        public class VerifyReversals
        {
            [Fact]
            public async void ShouldVerifyReversalsSettlements()
            {
                var chargeDate = new DateTime(2018, 11, 08);

                var charges = new List<ChargeProjection>()
                {
                    new ChargeProjection()
                    {
                        Status = OrdersApi.Domain.Model.ChargeAggregate.ChargeStatus.Processed,

                        OrderDetails = new OrdersApi.Domain.Model.ChargeAggregate.OrderDetails()
                        {
                            ChargeDate = chargeDate
                        },

                        Reversals = new List<ReversalProjection>()
                        {
                            new ReversalProjection()
                            {
                                Status = OrdersApi.Domain.Model.ChargeAggregate.ChargeStatus.Processed
                            }
                        }
                    },
                    new ChargeProjection()
                    {
                        Status = OrdersApi.Domain.Model.ChargeAggregate.ChargeStatus.Processed,

                        OrderDetails = new OrdersApi.Domain.Model.ChargeAggregate.OrderDetails()
                        {
                            ChargeDate = chargeDate
                        },

                        Reversals = new List<ReversalProjection>()
                        {
                            new ReversalProjection()
                            {
                                Status = OrdersApi.Domain.Model.ChargeAggregate.ChargeStatus.Processed
                            }
                        }
                    }

                }.AsEnumerable();

                var fixture = new Fixture().Customize(new AutoMoqCustomization());

                var repository = fixture.Freeze<Mock<IQueryableRepository<ChargeProjection>>>();

                Func<ISpecification<ChargeProjection>, Expression<Func<ChargeProjection, object>>, int?, Task<IEnumerable<ChargeProjection>>> getFilteredSortByAsyncMock = (specification, sort, limit) =>
                {
                    var filteredCharges = charges
                                            .Where(specification.SatisfiedBy().Compile())
                                                .OrderBy(sort.Compile())
                                                    .AsEnumerable();

                    return Task.FromResult(filteredCharges);
                };

                repository.Setup(bus => bus.GetFilteredSortByAsync(It.IsAny<ISpecification<ChargeProjection>>(),
                                                                   It.IsAny<Expression<Func<ChargeProjection, object>>>(),
                                                                   It.IsAny<int?>())).Returns(getFilteredSortByAsyncMock);

                var service = fixture.Create<Mock<AcquirerSettlementVerificationService>>();

                service.Setup(x => x.SendVerifySettlementCommandForReversal(It.IsAny<ChargeProjection>(), It.IsAny<ReversalProjection>())).Returns(Task.CompletedTask);
                service.SetupGet(x => x.ChargeDate).Returns(chargeDate);

                await service.Object.VerifyReversals();

                var reversals = charges.SelectMany(x => x.Reversals).Count();

                // Assertions
                service.Verify(x => x.SendVerifySettlementCommandForReversal(It.IsAny<ChargeProjection>(), It.IsAny<ReversalProjection>()), Times.Exactly(reversals));
            }
        }

        public class SendVerifySettlementCommandForCharge
        {
            [Fact]
            public async void ShouldScheduleSettlementVerification()
            {
                DateTime? chargeDate = new DateTime(2018, 11, 08);

                var settlementVerificationDate = chargeDate.Value.AddDays(1);

                var charge = new ChargeProjection()
                {
                    OrderDetails = new OrdersApi.Domain.Model.ChargeAggregate.OrderDetails()
                    {
                        ChargeDate = chargeDate.Value
                    }
                };

                var fixture = new Fixture().Customize(new AutoMoqCustomization());

                var commandBus = fixture.Freeze<Mock<ICommandBus>>();
                commandBus.Setup(bus => bus.Publish(It.IsAny<ICommand>(), It.IsAny<DateTime?>())).Returns(Task.CompletedTask);

                var service = fixture.Create<Mock<AcquirerSettlementVerificationService>>();
                service.SetupGet(x => x.ChargeDate).Returns(chargeDate.Value);

                await service.Object.SendVerifySettlementCommandForCharge(charge);

                // Assertions
                commandBus.Verify(bus => bus.Publish(It.IsAny<ICommand>(), settlementVerificationDate));

                charge.LastSettlementVerificationDate.Should().Be(settlementVerificationDate);
            }
        }

        public class SendVerifySettlementCommandForReversal
        {
            [Fact]
            public async void ShouldScheduleSettlementReversalVerification()
            {
                DateTime? chargeDate = new DateTime(2018, 11, 08);

                var settlementVerificationDate = chargeDate.Value.AddDays(1);

                var charge = new ChargeProjection()
                {
                    OrderDetails = new OrdersApi.Domain.Model.ChargeAggregate.OrderDetails()
                    {
                        ChargeDate = chargeDate.Value
                    }
                };

                var reversal = new ReversalProjection();

                var fixture = new Fixture().Customize(new AutoMoqCustomization());

                var commandBus = fixture.Freeze<Mock<ICommandBus>>();
                commandBus.Setup(bus => bus.Publish(It.IsAny<ICommand>(), It.IsAny<DateTime?>())).Returns(Task.CompletedTask);

                var service = fixture.Create<Mock<AcquirerSettlementVerificationService>>();
                service.SetupGet(x => x.ChargeDate).Returns(chargeDate.Value);

                await service.Object.SendVerifySettlementCommandForReversal(charge, reversal);

                // Assertions
                commandBus.Verify(bus => bus.Publish(It.IsAny<ICommand>(), settlementVerificationDate));

                reversal.LastSettlementVerificationDate.Should().Be(settlementVerificationDate);
            }
        }
    }    
}
