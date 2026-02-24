using ERMS.Application.Features.RecruitmentPlans.Commands.SubmitPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Commands.SubmitPlan
{
    public class SubmitPlanHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<SubmitPlanHandler>> _loggerMock;
        private readonly SubmitPlanHandler _handler;

        public SubmitPlanHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<SubmitPlanHandler>>();

            _handler = new SubmitPlanHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotDepartmentHead()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Candidate });
            var command = new SubmitPlanCommand { PlanId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Department Head mới có quyền submit kế hoạch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanHasNoPlanDetails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CreatedById = userId,
                Status = PlanStatus.Draft,
                TotalBudget = 1000,
                PlanCode = "P1",
                PlanName = "Empty Plan",
                Campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), Status = CampaignStatus.Open, CampaignName = "C1", CampaignCode = "C1", EnterpriseId = enterpriseId, CreatedById = userId }
            });
            await _context.SaveChangesAsync();

            var command = new SubmitPlanCommand { PlanId = planId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Kế hoạch phải có ít nhất 1 chi tiết tuyển dụng trước khi submit.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanHasNoBudget()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CreatedById = userId,
                Status = PlanStatus.Draft,
                TotalBudget = 0,
                PlanCode = "P1",
                PlanName = "No Budget Plan",
                Campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), Status = CampaignStatus.Open, CampaignName = "C1", CampaignCode = "C1", EnterpriseId = enterpriseId, CreatedById = userId },
                PlanDetails = new List<PlanDetail> { new PlanDetail { Id = Guid.NewGuid(), PositionTitle = "T", RequestedById = userId } }
            });
            await _context.SaveChangesAsync();

            var command = new SubmitPlanCommand { PlanId = planId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Kế hoạch phải có ngân sách (TotalBudget > 0) trước khi submit.");
        }

        [Fact]
        public async Task Handle_ShouldSubmitPlan_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var planDetail = new PlanDetail { Id = Guid.NewGuid(), PositionTitle = "T", RequestedById = userId };

            _context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CreatedById = userId,
                Status = PlanStatus.Draft,
                TotalBudget = 10000,
                PlanCode = "P1",
                PlanName = "Valid Plan",
                Campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), Status = CampaignStatus.Open, CampaignName = "C1", CampaignCode = "C1", EnterpriseId = enterpriseId, CreatedById = userId },
                PlanDetails = new List<PlanDetail> { planDetail }
            });
            await _context.SaveChangesAsync();

            var command = new SubmitPlanCommand { PlanId = planId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            var updatedPlan = await _context.RecruitmentPlans.FindAsync(planId);
            updatedPlan!.Status.Should().Be(PlanStatus.Pending);
            
            var updatedDetail = await _context.PlanDetails.FindAsync(planDetail.Id);
            updatedDetail!.Status.Should().Be(PlanDetailStatus.Pending);
        }
    }
}
