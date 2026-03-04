using ERMS.Application.Features.PlanDetails.Commands.CreatePlanDetail;
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

namespace ERMS.UnitTests.Features.PlanDetails.Commands.CreatePlanDetail
{
    public class CreatePlanDetailHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CreatePlanDetailHandler>> _loggerMock;
        private readonly CreatePlanDetailHandler _handler;

        public CreatePlanDetailHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CreatePlanDetailHandler>>();

            _handler = new CreatePlanDetailHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotDepartmentHead()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Candidate });
            var command = new CreatePlanDetailCommand { RecruitmentPlanId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Department Head mới có quyền tạo chi tiết kế hoạch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenSalaryRangeIsInvalid()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new CreatePlanDetailCommand 
            { 
                RecruitmentPlanId = Guid.NewGuid(),
                Quantity = 1,
                SalaryRangeMin = 100,
                SalaryRangeMax = 50
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("SalaryRangeMax phải lớn hơn hoặc bằng SalaryRangeMin.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanStatusIsInvalid()
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
                Status = PlanStatus.Pending,
                PlanCode = "P1",
                PlanName = "Pending Plan",
                Campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), Status = CampaignStatus.Open, CampaignName = "C1", CampaignCode = "C1", EnterpriseId = enterpriseId, CreatedById = userId }
            });
            await _context.SaveChangesAsync();

            var command = new CreatePlanDetailCommand { RecruitmentPlanId = planId, Quantity = 1, PositionTitle = "T" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Chỉ có thể thêm chi tiết khi kế hoạch ở trạng thái 'Draft' hoặc 'Rejected'*");
        }

        [Fact]
        public async Task Handle_ShouldCreatePlanDetail_AndUpdateTotalBudget_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CreatedById = userId,
                Status = PlanStatus.Draft,
                PlanCode = "P1",
                PlanName = "Draft Plan",
                TotalBudget = 0,
                Campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), Status = CampaignStatus.Open, CampaignName = "C1", CampaignCode = "C1", EnterpriseId = enterpriseId, CreatedById = userId }
            };
            _context.RecruitmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            var command = new CreatePlanDetailCommand 
            { 
                RecruitmentPlanId = planId, 
                Quantity = 2, 
                SalaryRangeMax = 5000000,
                PositionTitle = "Software Engineer"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            var detail = await _context.PlanDetails.FindAsync(result);
            detail.Should().NotBeNull();
            detail!.PositionTitle.Should().Be("Software Engineer");
            detail.Status.Should().Be(PlanDetailStatus.Pending);
            
            var updatedPlan = await _context.RecruitmentPlans.FindAsync(planId);
            updatedPlan!.TotalBudget.Should().Be(10000000); // 2 * 5M
        }
    }
}
