using ERMS.Application.Features.PlanDetails.Commands.DeletePlanDetail;
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

namespace ERMS.UnitTests.Features.PlanDetails.Commands.DeletePlanDetail
{
    public class DeletePlanDetailHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<DeletePlanDetailHandler>> _loggerMock;
        private readonly DeletePlanDetailHandler _handler;

        public DeletePlanDetailHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<DeletePlanDetailHandler>>();

            _handler = new DeletePlanDetailHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanDetailNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new DeletePlanDetailCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy chi tiết kế hoạch hoặc bạn không có quyền xóa.");
        }

        [Fact]
        public async Task Handle_ShouldDeleteDetail_AndRecalculateBudget_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var detailId1 = Guid.NewGuid();
            var detailId2 = Guid.NewGuid();

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
                TotalBudget = 10000000,
                Campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), Status = CampaignStatus.Open, CampaignName = "C1", CampaignCode = "C1", EnterpriseId = enterpriseId, CreatedById = userId }
            };
            _context.RecruitmentPlans.Add(plan);

            var detail1 = new PlanDetail
            {
                Id = detailId1,
                RecruitmentPlanId = planId,
                RequestedById = userId,
                PositionTitle = "Pos 1",
                Quantity = 1,
                SalaryRangeMax = 5000000,
                Status = PlanDetailStatus.Pending
            };
            var detail2 = new PlanDetail
            {
                Id = detailId2,
                RecruitmentPlanId = planId,
                RequestedById = userId,
                PositionTitle = "Pos 2",
                Quantity = 1,
                SalaryRangeMax = 5000000,
                Status = PlanDetailStatus.Pending
            };
            _context.PlanDetails.AddRange(detail1, detail2);
            await _context.SaveChangesAsync();

            var command = new DeletePlanDetailCommand { Id = detailId1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            var updatedDetail = await _context.PlanDetails.FindAsync(detailId1);
            updatedDetail!.IsDeleted.Should().BeTrue();
            
            var updatedPlan = await _context.RecruitmentPlans.FindAsync(planId);
            updatedPlan!.TotalBudget.Should().Be(5000000); // Only detail 2 remains
        }
    }
}
