using ERMS.Application.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Commands.CreateRecruitmentPlan
{
    public class CreateRecruitmentPlanHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CreateRecruitmentPlanHandler>> _loggerMock;
        private readonly CreateRecruitmentPlanHandler _handler;

        public CreateRecruitmentPlanHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CreateRecruitmentPlanHandler>>();

            _handler = new CreateRecruitmentPlanHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseDoesNotExist()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
            var command = new CreateRecruitmentPlanCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Doanh nghiệp không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanCodeExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Enterprises.Add(new Enterprise { Id = enterpriseId, EnterpriseName = "Ent", EnterpriseCode = "E1" });
            _context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanCode = "EXISTING",
                PlanName = "Existing",
                CreatedById = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            var command = new CreateRecruitmentPlanCommand { PlanCode = "EXISTING" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Mã kế hoạch tuyển dụng đã tồn tại trong doanh nghiệp");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCampaignNotFound()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Enterprises.Add(new Enterprise { Id = enterpriseId, EnterpriseName = "Ent", EnterpriseCode = "E1" });
            await _context.SaveChangesAsync();

            var command = new CreateRecruitmentPlanCommand { PlanCode = "NEW", CampaignId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Chiến dịch tuyển dụng không tồn tại hoặc không thuộc doanh nghiệp của bạn.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCampaignStatusIsNotOpen()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Enterprises.Add(new Enterprise { Id = enterpriseId, EnterpriseName = "Ent", EnterpriseCode = "E1" });
            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                Status = CampaignStatus.Draft,
                CampaignCode = "C1",
                CampaignName = "Draft Campaign",
                CreatedById = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            var command = new CreateRecruitmentPlanCommand { PlanCode = "NEW", CampaignId = campaignId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Chiến dịch phải ở trạng thái 'Open' để tạo kế hoạch. Trạng thái hiện tại: {CampaignStatus.Draft}");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDatesAreInvalid()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Enterprises.Add(new Enterprise { Id = enterpriseId, EnterpriseName = "Ent", EnterpriseCode = "E1" });
            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                Status = CampaignStatus.Open, // Assuming Active is Open (CanSubmitPlans needs verification)
                CampaignCode = "C1",
                CampaignName = "Open Campaign",
                CreatedById = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            var command = new CreateRecruitmentPlanCommand
            {
                PlanCode = "NEW",
                CampaignId = campaignId,
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5)
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu");
        }

        [Fact]
        public async Task Handle_ShouldCreatePlan_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            _context.Enterprises.Add(new Enterprise { Id = enterpriseId, EnterpriseName = "Ent", EnterpriseCode = "E1" });
            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                Status = CampaignStatus.Open,
                CampaignCode = "C1",
                CampaignName = "Open Campaign",
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new CreateRecruitmentPlanCommand
            {
                PlanName = "New Plan",
                PlanCode = "NEW-PLAN",
                CampaignId = campaignId,
                DepartmentId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                TotalBudget = 5000000
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            var plan = await _context.RecruitmentPlans.FindAsync(result);
            plan.Should().NotBeNull();
            plan!.PlanName.Should().Be(command.PlanName);
            plan.Status.Should().Be(PlanStatus.Draft);
            plan.CreatedById.Should().Be(userId);
        }
    }
}
