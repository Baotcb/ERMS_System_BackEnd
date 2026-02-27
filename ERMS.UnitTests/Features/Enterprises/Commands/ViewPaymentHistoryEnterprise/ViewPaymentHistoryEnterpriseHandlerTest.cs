using ERMS.Application.Features.Enterprises.Commands.ViewPaymentHistoryEnterprise;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.EnterpriseStatusConstants.Commands.ViewPaymentHistoryEnterprise
{
    public class ViewPaymentHistoryEnterpriseHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly ViewPaymentHistoryEnterpriseHandler _handler;

        public ViewPaymentHistoryEnterpriseHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new ViewPaymentHistoryEnterpriseHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenDirectorMismatch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EnterpriseId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                DepartmentId = 1
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var command = new ViewPaymentHistoryEnterpriseCommand { EnterpriseId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have permission to view payment history for this enterprise.");
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenNoHistoryFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Admin });
            
            var command = new ViewPaymentHistoryEnterpriseCommand { EnterpriseId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("No payment history found for the specified enterprise.");
        }

        [Fact]
        public async Task Handle_ShouldReturnHistory_WhenSuccessful()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Admin });

            var plan = new SubscriptionPlan 
            { 
                Id = Guid.NewGuid(), 
                PlanName = "Pro", 
                PlanCode = "PLAN_PRO",
                IsDeleted = false 
            };
            var prevPlan = new SubscriptionPlan 
            { 
                Id = Guid.NewGuid(), 
                PlanName = "Free", 
                PlanCode = "PLAN_FREE",
                IsDeleted = false 
            };
            
            var history = new SubscriptionHistory 
            { 
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                PreviousPlanId = prevPlan.Id,
                PreviousPlan = prevPlan,
                ActionType = "Upgrade",
                PeriodStartDate = DateTime.UtcNow
            };
            
            _context.SubscriptionPlans.AddRange(plan, prevPlan);
            _context.SubscriptionHistories.Add(history);
            await _context.SaveChangesAsync();

            var command = new ViewPaymentHistoryEnterpriseCommand { EnterpriseId = enterpriseId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.SubscriptionHistories.Should().HaveCount(1);
            result.SubscriptionHistories.First().PlanName.Should().Be("Pro");
            result.SubscriptionHistories.First().PreviousPlanName.Should().Be("Free");
        }
    }
}
