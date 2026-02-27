using ERMS.Application.Features.Enterprises.Commands.RegisterEnterprise;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.EnterpriseStatusConstants.Commands.RegisterEnterprise
{
    public class RegisterEnterpriseHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly RegisterEnterpriseHandler _handler;
        private readonly Mock<DbSet<ERMS.Domain.Entities.Enterprise.Enterprise>> _enterprisesMock;
        private readonly Mock<DbSet<SubscriptionPlan>> _subscriptionPlansMock;

        public RegisterEnterpriseHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();

            // Mock DbSets
            _enterprisesMock = CreateMockDbSet(new List<ERMS.Domain.Entities.Enterprise.Enterprise>());
            _subscriptionPlansMock = CreateMockDbSet(new List<SubscriptionPlan>());

            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);
            _contextMock.Setup(x => x.SubscriptionPlans).Returns(_subscriptionPlansMock.Object);

            _handler = new RegisterEnterpriseHandler(_contextMock.Object);
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            return mockSet;
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenTaxCodeExists()
        {
            // Arrange
            var existingEnterprise = new ERMS.Domain.Entities.Enterprise.Enterprise 
            { 
                TaxCode = "123456789", 
                IsDeleted = false 
            };
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { existingEnterprise };
            var mockSet = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(mockSet.Object);

            var command = new RegisterEnterpriseCommand { TaxCode = "123456789" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Mã số thuế doanh nghiệp đã tồn tại.");
        }

        [Fact]
        public async Task Handle_ShouldCreateEnterpriseAndPlan_WhenSuccessful()
        {
            // Arrange
            var command = new RegisterEnterpriseCommand
            {
                EnterpriseName = "New Enterprise",
                TaxCode = "987654321",
                Email = "contact@newent.com",
                Phone = "0123456789",
                Address = "123 Main St",
                Website = "https://newent.com",
                LogoUrl = "https://newent.com/logo.png"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _enterprisesMock.Verify(x => x.Add(It.IsAny<ERMS.Domain.Entities.Enterprise.Enterprise>()), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            
            // Verify Subscription Plan creation (since list is empty initially)
            // Logic: if FREE plan not found, create it.
            _subscriptionPlansMock.Verify(x => x.Add(It.IsAny<SubscriptionPlan>()), Times.Once); 
        }

        [Fact]
        public async Task Handle_ShouldUseExistingPlan_WhenPlanExists()
        {
             // Arrange
            var freePlan = new SubscriptionPlan { PlanCode = "FREE", IsDeleted = false, Id = Guid.NewGuid() };
            var plans = new List<SubscriptionPlan> { freePlan };
            var mockPlans = CreateMockDbSet(plans);
            _contextMock.Setup(x => x.SubscriptionPlans).Returns(mockPlans.Object);

            var command = new RegisterEnterpriseCommand
            {
                EnterpriseName = "New Enterprise 2",
                Email = "contact2@newent.com"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _enterprisesMock.Verify(x => x.Add(It.IsAny<ERMS.Domain.Entities.Enterprise.Enterprise>()), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            
            // Verify NO NEW Subscription Plan creation
            _subscriptionPlansMock.Verify(x => x.Add(It.IsAny<SubscriptionPlan>()), Times.Never);
        }
    }
}
