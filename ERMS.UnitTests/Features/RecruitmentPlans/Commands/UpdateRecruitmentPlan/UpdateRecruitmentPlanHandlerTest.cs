using ERMS.Application.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Commands.UpdateRecruitmentPlan
{
    public class UpdateRecruitmentPlanHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<UpdateRecruitmentPlanHandler>> _loggerMock;
        private readonly UpdateRecruitmentPlanHandler _handler;

        public UpdateRecruitmentPlanHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<UpdateRecruitmentPlanHandler>>();

            _handler = new UpdateRecruitmentPlanHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
            var command = new UpdateRecruitmentPlanCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanCodeExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var planId1 = Guid.NewGuid();
            var planId2 = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.RecruitmentPlans.AddRange(new List<RecruitmentPlan>
            {
                new RecruitmentPlan { Id = planId1, EnterpriseId = enterpriseId, PlanCode = "CODE1", PlanName = "P1", CreatedById = Guid.NewGuid() },
                new RecruitmentPlan { Id = planId2, EnterpriseId = enterpriseId, PlanCode = "CODE2", PlanName = "P2", CreatedById = Guid.NewGuid() }
            });
            await _context.SaveChangesAsync();

            var command = new UpdateRecruitmentPlanCommand 
            { 
                Id = planId1, 
                PlanCode = "CODE2", // Duplicate with plan 2
                PlanName = "Updated P1" 
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Mã kế hoạch tuyển dụng đã tồn tại trong doanh nghiệp");
        }

        [Fact]
        public async Task Handle_ShouldUpdatePlan_WhenSuccessful()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var plan = new RecruitmentPlan 
            { 
                Id = planId, 
                EnterpriseId = enterpriseId, 
                PlanCode = "OLD", 
                PlanName = "Old Name", 
                CreatedById = Guid.NewGuid(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            _context.RecruitmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            var command = new UpdateRecruitmentPlanCommand
            {
                Id = planId,
                PlanName = "New Name",
                PlanCode = "NEW",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(5),
                TotalBudget = 1000,
                Description = "New Desc"
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedPlan = await _context.RecruitmentPlans.FindAsync(planId);
            updatedPlan!.PlanName.Should().Be("New Name");
            updatedPlan.PlanCode.Should().Be("NEW");
            updatedPlan.Description.Should().Be("New Desc");
            updatedPlan.TotalBudget.Should().Be(1000);
        }
    }
}
