using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using OrdersApi.Cqrs.Models;
using OrdersApi.Domain.Events.Charge;
using OrdersApi.Domain.Events.Charge.Reversal;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.Snapshots;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Domain.Model
{
    public class ChargeTests
    {
        [Fact]
        public void CanVerifySettlementReturnsFalseWhenStatusIsSettled()
        {
            var charge = new Fixture().Create<Charge>();
            charge.Status = ChargeStatus.Settled;
            Assert.False(charge.CanVerifySettlement);
        }


        [Fact]
        public void CanVerifySettlementReturnsFalseWhenStatusIsError()
        {
            var charge = new Fixture().Create<Charge>();
            charge.Status = ChargeStatus.Error;
            Assert.False(charge.CanVerifySettlement);
        }

        [Fact]
        public void ConstructorAppliesCreatedEventTest()
        { 
            var charge = new Mock<Charge>(null, null, null, null, null, null) {CallBase = true};
            var constructed = charge.Object;
            charge.Verify(x => x.OnChargeCreated(It.IsAny<ChargeCreated>()), Times.Once);
        }

        [Fact]
        public void OnCreatedTest()
        {
            var fixture = new Fixture(); 
            var charge = new Charge();
            var eventData = fixture.Create<ChargeCreated>();
            charge.OnChargeCreated(eventData);
            Assert.Equal(eventData.EventCommittedTimestamp, charge.CreatedDate); 
            Assert.Equal(eventData.AggregateKey, charge.AggregateKey);
            Assert.Equal(ChargeStatus.Created, charge.Status);
            Assert.Equal(eventData.OrderDetails, charge.OrderDetails);
            Assert.Equal(eventData.PaymentMethodData, charge.PaymentMethodData); 

        } 

        [Fact]
        public void ExpireAppliesEventTest()
        { 
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Object.Expire(null, null, null);
            charge.Verify(x => x.OnChargeExpired(It.IsAny<ChargeExpired>()), Times.Once);
        }

        [Fact]
        public void OnChargeProcessedTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<ChargeProcessed>();

            eventData.Result = new IntegrationResult(Result.Sucess);
            charge.OnChargeProcessed(eventData);
            Assert.Equal(ChargeStatus.Processed, charge.Status);


            eventData.Result = new IntegrationResult(Result.Error);
            charge.OnChargeProcessed(eventData);
            Assert.Equal(ChargeStatus.Rejected, charge.Status);
        }

        [Fact]
        public void OnNotSettledTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var currentStatus = charge.Status;
            var eventData = fixture.Create<ChargeNotSettled>();
            charge.OnChargeNotSettled(eventData);
            Assert.Equal(currentStatus, charge.Status); 

        }

        [Fact]
        public void OnSettledTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<ChargeSettled>();
            charge.OnChargeSettled(eventData);
            Assert.Equal(ChargeStatus.Settled, charge.Status);
            Assert.Equal(eventData.SettlementDate, charge.SettlementDate);

        }

        [Fact]
        public void OnChargeExpiredTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<ChargeExpired>();
            charge.OnChargeExpired(eventData); 
            Assert.Equal(ChargeStatus.Error, charge.Status); 

        }

        [Fact]
        public void OnChargeCouldNotBeProcessedTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var currentStatus = charge.Status;
            var eventData = fixture.Create<ChargeCouldNotBeProcessed>();
            charge.OnChargeCouldNotBeProcessed(eventData);
            Assert.Equal(currentStatus, charge.Status);

        }

        [Fact]
        public void TakeSnapshotTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var snap = charge.TakeSnapshot() as ChargeSnapshot;
            Assert.NotNull(snap);
            Assert.Equal(charge.AggregateKey, snap.AggregateKey);
            Assert.Equal(charge.OrderDetails, snap.OrderDetails);
            Assert.Equal(charge.PaymentMethodData, snap.PaymentMethodData);
            Assert.False(string.IsNullOrWhiteSpace(snap.SnapshotKey));
             
        }

        [Fact]
        public void ApplySnapshotTest()
        {
            var fixture = new Fixture();
            var app = fixture.Create<Charge>();
            var snap = fixture.Create<ChargeSnapshot>();
            app.ApplySnapshot(snap); 
            Assert.Equal(snap.Version, app.CurrentVersion);
            Assert.Equal(snap.Status, app.Status);
            Assert.Equal(snap.AggregateKey, app.AggregateKey);
            Assert.Equal(snap.OrderDetails, app.OrderDetails);
            Assert.Equal(snap.PaymentMethodData, app.PaymentMethodData);
        }

        [Fact]
        public void SendToAcquirerExpiredTest()
        { 
            var charge = new Mock<Charge>() {CallBase = true};
            charge.Setup(x => x.Status).Returns(ChargeStatus.Error);
            var integrationService = new Mock<IAcquirerApiService>();

            charge.Object.SendToAcquirer(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeCouldNotBeProcessed(It.IsAny<ChargeCouldNotBeProcessed>()), Times.Never);
            charge.Verify(x => x.OnChargeProcessed(It.IsAny<ChargeProcessed>()), Times.Never);
        }

        [Fact]
        public void SendToAcquirerCouldNotCheckIdAlreadySentTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Error)));

            charge.Object.SendToAcquirer(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeCouldNotBeProcessed(It.IsAny<ChargeCouldNotBeProcessed>()), Times.Once);
            charge.Verify(x => x.OnChargeProcessed(It.IsAny<ChargeProcessed>()), Times.Never);
        }

        [Fact]
        public void SendToAcquirerAlreadySentTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Sucess)
            {
                ReturnedObject = true
            }));

            charge.Object.SendToAcquirer(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeCouldNotBeProcessed(It.IsAny<ChargeCouldNotBeProcessed>()), Times.Never);
            charge.Verify(x => x.OnChargeProcessed(It.IsAny<ChargeProcessed>()), Times.Once);
        }

        [Fact]
        public void SendToAcquirerFailsTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Sucess)
            {
                ReturnedObject = false
            }));
            integrationService.Setup(x => x.SendChargeOrder(charge.Object)).Returns(Task.FromResult(new IntegrationResult(Result.Error)));

            charge.Object.SendToAcquirer(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeCouldNotBeProcessed(It.IsAny<ChargeCouldNotBeProcessed>()), Times.Once);
            charge.Verify(x => x.OnChargeProcessed(It.IsAny<ChargeProcessed>()), Times.Never);
        }

        [Fact]
        public void SendToAcquirerTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Sucess)
            {
                ReturnedObject = false
            }));
            integrationService.Setup(x => x.SendChargeOrder(charge.Object)).Returns(Task.FromResult(new IntegrationResult(Result.Sucess)));

            charge.Object.SendToAcquirer(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeCouldNotBeProcessed(It.IsAny<ChargeCouldNotBeProcessed>()), Times.Never);
            charge.Verify(x => x.OnChargeProcessed(It.IsAny<ChargeProcessed>()), Times.Once);
        }

        [Fact]
        public void VerifySettlementReturnsNullTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(default(IntegrationResult<DateTime?>)));

            charge.Object.VerifySettlement(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeSettled(It.IsAny<ChargeSettled>()), Times.Never);
            charge.Verify(x => x.OnChargeNotSettled(It.IsAny<ChargeNotSettled>()), Times.Once);
        }

        [Fact]
        public void VerifySettlementReturnsUnsettledTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<DateTime?>(Result.Sucess)
            {
                ReturnedObject = null
            }));

            charge.Object.VerifySettlement(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeSettled(It.IsAny<ChargeSettled>()), Times.Never);
            charge.Verify(x => x.OnChargeNotSettled(It.IsAny<ChargeNotSettled>()), Times.Once);
        }

        [Fact]
        public void VerifySettlementIntegrationErrorTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<DateTime?>(Result.Error)));

            charge.Object.VerifySettlement(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeSettled(It.IsAny<ChargeSettled>()), Times.Never);
            charge.Verify(x => x.OnChargeNotSettled(It.IsAny<ChargeNotSettled>()), Times.Once);
        }

        [Fact]
        public void VerifySettlementTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
            var integrationService = new Mock<IAcquirerApiService>();
            integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<DateTime?>(Result.Sucess)
            {
                ReturnedObject = DateTime.Today
            })); 

            charge.Object.VerifySettlement(null, null, null, integrationService.Object);

            charge.Verify(x => x.OnChargeSettled(It.IsAny<ChargeSettled>()), Times.Once);
            charge.Verify(x => x.OnChargeNotSettled(It.IsAny<ChargeNotSettled>()), Times.Never);
        }

        [Fact]
        public void CanNotRevertIfNotProcessedTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Created); 
            Assert.False(charge.Object.CanRevert());
        }

        [Fact]
        public void CanRevertIfProcessedAndAmountNotFullyRevertedTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.OrderDetails).Returns(new OrderDetails() { Amount = 120 });
            charge.Setup(x => x.Status).Returns(ChargeStatus.Processed);
            Assert.True(charge.Object.CanRevert());
        }

        [Fact]
        public void CanRevertIfSettledAndAmountNotFullyRevertedTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.OrderDetails).Returns(new OrderDetails() { Amount = 120 });
            charge.Setup(x => x.Status).Returns(ChargeStatus.Settled);
            Assert.True(charge.Object.CanRevert());
        }

        [Fact]
        public void CanNotRevertIfProcessedAndAmountFullyRevertedTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.Status).Returns(ChargeStatus.Processed);
            charge.Setup(x => x.OrderDetails).Returns(new OrderDetails(){ Amount = 120 });
            charge.Setup(x => x.Reversals).Returns(new List<Reversal>()
            {
                new Reversal() {Amount = 50}
            });
            Assert.True(charge.Object.CanRevert(100));
        }

        [Fact]
        public void OnAcquirerAccountReversalProcessedTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<AcquirerAccountReversalProcessed>();
            var reversal = new Reversal()
            {
                ReversalKey = eventData.ReversalKey
            };
            charge.Reversals = new List<Reversal>()
            {
                reversal
            };

            eventData.Result = new IntegrationResult(Result.Sucess);
            charge.OnAcquirerAccountReversalProcessed(eventData);
            Assert.Equal(ChargeStatus.Processed, reversal.Status);


            eventData.Result = new IntegrationResult(Result.Error);
            charge.OnAcquirerAccountReversalProcessed(eventData);
            Assert.Equal(ChargeStatus.Rejected, reversal.Status);
        }

        [Fact]
        public void OnAcquirerAccountReversalErrorTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<AcquirerAccountReversalError>();
            var reversal = new Reversal()
            {
                ReversalKey = eventData.ReversalKey
            };
            charge.Reversals = new List<Reversal>()
            {
                reversal
            };
             
            charge.OnAcquirerAccountReversalError(eventData);
            Assert.Equal(ChargeStatus.Error, reversal.Status); 
        }


        [Fact]
        public void OnReversalSettledTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<ReversalSettled>();
            var reversal = new Reversal()
            {
                ReversalKey = eventData.ReversalKey
            };
            charge.Reversals = new List<Reversal>()
            {
                reversal
            };

            charge.OnReversalSettled(eventData);
            Assert.Equal(ChargeStatus.Settled, reversal.Status);
        }

        [Fact]
        public void OnReversalNotSettledTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<ReversalNotSettled>();
            var reversal = new Reversal()
            {
                ReversalKey = eventData.ReversalKey
            };
            charge.Reversals = new List<Reversal>()
            {
                reversal
            };

            charge.OnReversalNotSettled(eventData);
            Assert.Equal(ChargeStatus.Processed, reversal.Status);
        }

        [Fact]
        public void OnChargeReversalCreatedTest()
        {
            var fixture = new Fixture();
            var charge = fixture.Create<Charge>();
            var eventData = fixture.Create<ReversalCreated>();
            var expecetdCount = charge.Reversals.Count + 1;

            charge.OnChargeReversalCreated(eventData);
            Assert.Equal(expecetdCount, charge.Reversals.Count);
        }

        [Fact]
        public void GetReversalDateReturnsD1()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.OrderDetails).Returns(new OrderDetails()
            {
                ChargeDate = DateTime.UtcNow.Date.AddDays(-3)
            });

            var date = charge.Object.GetReversalDate();

            Assert.Equal(DateTime.UtcNow.Date.AddDays(1), date);
        }


        [Fact]
        public void GetReversalDateReturnsOrderDate()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.OrderDetails).Returns(new OrderDetails()
            {
                ChargeDate = DateTime.UtcNow.Date.AddDays(3)
            });

            var date = charge.Object.GetReversalDate();

            Assert.Equal(DateTime.UtcNow.Date.AddDays(3), date);
        }

        [Fact]
        public void RevertAppliesEventTest()
        {
            var charge = new Mock<Charge>() { CallBase = true };
            charge.Setup(x => x.OrderDetails).Returns(new OrderDetails()
            {
                ChargeDate = DateTime.UtcNow.AddDays(3)
            });
            charge.Object.Revert(null, null, null, 10, null);
            charge.Verify(x => x.GetReversalDate(), Times.Once);
            charge.Verify(x => x.OnChargeReversalCreated(It.IsAny<ReversalCreated>()), Times.Once);
        }

        public class ProcessAcquirerAccountReversalTests
        {
            [Fact]
            public void SendToAcquirerAlreadySentTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Sucess)
                {
                    ReturnedObject = true
                }));

                charge.Object.SendReversalToAcquirer(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnAcquirerAccountReversalProcessed(It.IsAny<AcquirerAccountReversalProcessed>()), Times.Once);
                charge.Verify(x => x.OnAcquirerAccountReversalError(It.IsAny<AcquirerAccountReversalError>()), Times.Never);
            }

            [Fact]
            public void SendToAcquirerFailsTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Sucess)
                {
                    ReturnedObject = false
                }));
                integrationService.Setup(x => x.SendReversalOrder(charge.Object, It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult(Result.Error)));

                charge.Object.SendReversalToAcquirer(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnAcquirerAccountReversalError(It.IsAny<AcquirerAccountReversalError>()), Times.Once);
                charge.Verify(x => x.OnAcquirerAccountReversalProcessed(It.IsAny<AcquirerAccountReversalProcessed>()), Times.Never);
            }

            [Fact]
            public void SendToAcquirerTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.CheckIfChargeOrderWasSent(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<bool>(Result.Sucess)
                {
                    ReturnedObject = false
                }));
                integrationService.Setup(x => x.SendReversalOrder(charge.Object, It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult(Result.Sucess)));

                charge.Object.SendReversalToAcquirer(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnAcquirerAccountReversalError(It.IsAny<AcquirerAccountReversalError>()), Times.Never);
                charge.Verify(x => x.OnAcquirerAccountReversalProcessed(It.IsAny<AcquirerAccountReversalProcessed>()), Times.Once);
            }
        }

        public class VerifyAcquirerAccountReversalSettlementTests
        {

            [Fact]
            public void VerifySettlementReturnsNullTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                charge.Setup(x => x.Reversals).Returns(new List<Reversal>()
                {
                    new Reversal()
                    {
                        ReversalKey = "XPTO",
                        Status = ChargeStatus.Processed
                    }
                });
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(default(IntegrationResult<DateTime?>)));

                charge.Object.VerifyReversalSettlement(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnReversalSettled(It.IsAny<ReversalSettled>()), Times.Never);
                charge.Verify(x => x.OnReversalNotSettled(It.IsAny<ReversalNotSettled>()), Times.Once);
            }

            [Fact]
            public void VerifySettlementReturnsUnsettledTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                charge.Setup(x => x.Reversals).Returns(new List<Reversal>()
                {
                    new Reversal()
                    {
                        ReversalKey = "XPTO",
                        Status = ChargeStatus.Processed
                    }
                });
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<DateTime?>(Result.Sucess)
                {
                    ReturnedObject = null
                }));

                charge.Object.VerifyReversalSettlement(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnReversalSettled(It.IsAny<ReversalSettled>()), Times.Never);
                charge.Verify(x => x.OnReversalNotSettled(It.IsAny<ReversalNotSettled>()), Times.Once);
            }

            [Fact]
            public void VerifySettlementIntegrationErrorTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                charge.Setup(x => x.Reversals).Returns(new List<Reversal>()
                {
                    new Reversal()
                    {
                        ReversalKey = "XPTO",
                        Status = ChargeStatus.Processed
                    }
                });
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<DateTime?>(Result.Error)));

                charge.Object.VerifyReversalSettlement(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnReversalSettled(It.IsAny<ReversalSettled>()), Times.Never);
                charge.Verify(x => x.OnReversalNotSettled(It.IsAny<ReversalNotSettled>()), Times.Once);
            }

            [Fact]
            public void VerifySettlementTest()
            {
                var charge = new Mock<Charge>() { CallBase = true };
                charge.Setup(x => x.Status).Returns(ChargeStatus.Created);
                charge.Setup(x => x.Reversals).Returns(new List<Reversal>()
                {
                    new Reversal()
                    {
                        ReversalKey = "XPTO",
                        Status = ChargeStatus.Processed
                    }
                });
                var integrationService = new Mock<IAcquirerApiService>();
                integrationService.Setup(x => x.GetSettlementDate(It.IsAny<AcquirerAccount>(), It.IsAny<string>())).Returns(Task.FromResult(new IntegrationResult<DateTime?>(Result.Sucess)
                {
                    ReturnedObject = DateTime.Today
                }));

                charge.Object.VerifyReversalSettlement(null, null, null, "XPTO", integrationService.Object);

                charge.Verify(x => x.OnReversalSettled(It.IsAny<ReversalSettled>()), Times.Once);
                charge.Verify(x => x.OnReversalNotSettled(It.IsAny<ReversalNotSettled>()), Times.Never);
            }
        }
    }

  
}
