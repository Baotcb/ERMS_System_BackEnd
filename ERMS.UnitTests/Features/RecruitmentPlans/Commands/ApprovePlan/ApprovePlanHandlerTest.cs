using ERMS.Application.Features.RecruitmentPlans.Commands.ApprovePlan;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Commands.ApprovePlan
{
    public class ApprovePlanHandlerTest
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<ApprovePlanHandler>> _loggerMock;

        public ApprovePlanHandlerTest()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<ApprovePlanHandler>>();
        }

        private DbContextOptions<ERMSDbContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotDirector()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = Guid.NewGuid() };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Director mới có quyền phê duyệt kế hoạch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanNotFound()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = Guid.NewGuid() };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanStatusIsNotPending()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Draft,
                PlanCode = "P1",
                PlanName = "Draft Plan",
                CreatedById = userId,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = planId };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Chỉ có thể phê duyệt kế hoạch ở trạng thái 'Pending'*");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCampaignStatusIsNotOpen()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Closed,
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                PlanCode = "P1",
                PlanName = "Pending Plan",
                CreatedById = userId,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = planId };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Chiến dịch phải ở trạng thái 'Open'*");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenBudgetExceeded()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                TotalBudgetCeiling = 1000,
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                TotalBudget = 1500, // Exceeds 1000
                PlanCode = "P1",
                PlanName = "Expensive Plan",
                CreatedById = userId,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = planId };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*vượt quá ngân sách còn lại*");
        }

        [Fact]
        public async Task Handle_ShouldApprovePlan_WhenSuccessful()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var detailId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                TotalBudgetCeiling = 10000,
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                TotalBudget = 5000,
                PlanCode = "P1",
                PlanName = "Valid Plan",
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentPlans.Add(plan);

            var planDetail = new PlanDetail 
            { 
                Id = detailId, 
                RecruitmentPlanId = planId,
                Status = PlanDetailStatus.Pending,
                PositionTitle = "Title",
                RequestedById = userId,
                IsDeleted = false
            };
            context.PlanDetails.Add(planDetail);

            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = planId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var updatedPlan = await context.RecruitmentPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId);
            updatedPlan.Should().NotBeNull();
            updatedPlan!.Status.Should().Be(PlanStatus.Approved);
            updatedPlan.ApprovedById.Should().Be(userId);
            updatedPlan.ApprovedAt.Should().NotBeNull();

            var updatedDetail = await context.PlanDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == detailId);
            updatedDetail.Should().NotBeNull();
            updatedDetail!.Status.Should().Be(PlanDetailStatus.Approved);
        }

        [Fact]
        public async Task Handle_ShouldApproveMultipleDetails_WhenPlanHasMany()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                TotalBudgetCeiling = 20000,
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                TotalBudget = 10000,
                PlanCode = "P1",
                PlanName = "Multi Detail Plan",
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentPlans.Add(plan);

            var details = new List<PlanDetail>
            {
                new PlanDetail 
                { 
                    Id = Guid.NewGuid(), 
                    RecruitmentPlanId = planId,
                    Status = PlanDetailStatus.Pending,
                    PositionTitle = "Position 1",
                    RequestedById = userId,
                    IsDeleted = false
                },
                new PlanDetail 
                { 
                    Id = Guid.NewGuid(), 
                    RecruitmentPlanId = planId,
                    Status = PlanDetailStatus.Pending,
                    PositionTitle = "Position 2",
                    RequestedById = userId,
                    IsDeleted = false
                },
                new PlanDetail 
                { 
                    Id = Guid.NewGuid(), 
                    RecruitmentPlanId = planId,
                    Status = PlanDetailStatus.Pending,
                    PositionTitle = "Position 3",
                    RequestedById = userId,
                    IsDeleted = false
                }
            };
            context.PlanDetails.AddRange(details);
            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = planId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var approvedDetails = await context.PlanDetails
                .AsNoTracking()
                .Where(d => d.RecruitmentPlanId == planId)
                .ToListAsync();

            approvedDetails.Should().HaveCount(3);
            approvedDetails.Should().OnlyContain(d => d.Status == PlanDetailStatus.Approved);
        }

        [Fact]
        public async Task Handle_ShouldConsiderExistingApprovedPlans_WhenCheckingBudget()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                TotalBudgetCeiling = 10000,
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            // Existing approved plan (using 6000 of budget)
            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Approved,
                TotalBudget = 6000,
                PlanCode = "P_APPROVED",
                PlanName = "Already Approved Plan",
                CreatedById = userId,
                IsDeleted = false
            });

            // New plan to approve (needs 5000, but only 4000 remaining)
            var newPlanId = Guid.NewGuid();
            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = newPlanId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                TotalBudget = 5000,
                PlanCode = "P_NEW",
                PlanName = "New Plan",
                CreatedById = userId,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = newPlanId };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*vượt quá ngân sách còn lại*");
        }

        [Fact]
        public async Task Handle_ShouldNotApprove_WhenPlanBelongsToDifferentEnterprise()
        {
            // Arrange
            var options = CreateInMemoryOptions();
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var userEnterpriseId = Guid.NewGuid();
            var otherEnterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(userEnterpriseId);

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = otherEnterpriseId,
                CampaignName = "Other Campaign",
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                CreatedById = Guid.NewGuid(),
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = otherEnterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                PlanCode = "P1",
                PlanName = "Other Enterprise Plan",
                CreatedById = Guid.NewGuid(),
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var handler = new ApprovePlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new ApprovePlanCommand { PlanId = planId };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng.");
        }
    }
}
