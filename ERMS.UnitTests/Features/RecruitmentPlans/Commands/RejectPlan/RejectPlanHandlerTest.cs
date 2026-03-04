using ERMS.Application.Features.RecruitmentPlans.Commands.RejectPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Identity;
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

namespace ERMS.UnitTests.Features.RecruitmentPlans.Commands.RejectPlan
{
    public class RejectPlanHandlerTest
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<RejectPlanHandler>> _loggerMock;

        public RejectPlanHandlerTest()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<RejectPlanHandler>>();
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotDirector()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = Guid.NewGuid(), RejectionReason = "Reason" };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Director mới có quyền từ chối kế hoạch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenReasonIsMissing()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = Guid.NewGuid(), RejectionReason = "" };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Lý do từ chối là bắt buộc.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenReasonIsTooLong()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand 
            { 
                PlanId = Guid.NewGuid(), 
                RejectionReason = new string('a', 1001) // 1001 characters
            };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Lý do từ chối không được vượt quá 1000 ký tự.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Director });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = Guid.NewGuid(), RejectionReason = "Not found" };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanStatusIsNotPending()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
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

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = planId, RejectionReason = "Not good" };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Chỉ có thể từ chối kế hoạch ở trạng thái 'Pending'*");
        }

        [Fact]
        public async Task Handle_ShouldRejectPlan_WhenSuccessful()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
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

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = planId, RejectionReason = "Budget cut" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var updatedPlan = await context.RecruitmentPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId);
            updatedPlan.Should().NotBeNull();
            updatedPlan!.Status.Should().Be(PlanStatus.Rejected);
            updatedPlan.RejectionReason.Should().Be("Budget cut");
            updatedPlan.RejectedAt.Should().NotBeNull();

            var updatedDetail = await context.PlanDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == detailId);
            updatedDetail.Should().NotBeNull();
            updatedDetail!.Status.Should().Be(PlanDetailStatus.Rejected);
        }

        [Fact]
        public async Task Handle_ShouldRejectMultipleDetails_WhenPlanHasMany()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
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

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
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

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = planId, RejectionReason = "Not aligned with strategy" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var rejectedDetails = await context.PlanDetails
                .AsNoTracking()
                .Where(d => d.RecruitmentPlanId == planId)
                .ToListAsync();

            rejectedDetails.Should().HaveCount(3);
            rejectedDetails.Should().OnlyContain(d => d.Status == PlanDetailStatus.Rejected);
        }

        [Fact]
        public async Task Handle_ShouldNotRejectDeletedDetails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
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

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                PlanCode = "P1",
                PlanName = "Plan with deleted detail",
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentPlans.Add(plan);

            var activeDetail = new PlanDetail 
            { 
                Id = Guid.NewGuid(), 
                RecruitmentPlanId = planId,
                Status = PlanDetailStatus.Pending,
                PositionTitle = "Active Position",
                RequestedById = userId,
                IsDeleted = false
            };

            var deletedDetail = new PlanDetail 
            { 
                Id = Guid.NewGuid(), 
                RecruitmentPlanId = planId,
                Status = PlanDetailStatus.Pending,
                PositionTitle = "Deleted Position",
                RequestedById = userId,
                IsDeleted = true
            };

            context.PlanDetails.AddRange(activeDetail, deletedDetail);
            await context.SaveChangesAsync();

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = planId, RejectionReason = "Test reason" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var updatedActiveDetail = await context.PlanDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == activeDetail.Id);
            updatedActiveDetail!.Status.Should().Be(PlanDetailStatus.Rejected);

            var updatedDeletedDetail = await context.PlanDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == deletedDetail.Id);
            updatedDeletedDetail!.Status.Should().Be(PlanDetailStatus.Pending); // Should remain unchanged
        }

        [Fact]
        public async Task Handle_ShouldTrimRejectionReason()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
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

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                Status = PlanStatus.Pending,
                PlanCode = "P1",
                PlanName = "Plan",
                CreatedById = userId,
                IsDeleted = false
            };
            context.RecruitmentPlans.Add(plan);
            await context.SaveChangesAsync();

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = planId, RejectionReason = "  Budget cut  " };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var updatedPlan = await context.RecruitmentPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId);
            updatedPlan!.RejectionReason.Should().Be("Budget cut"); // Trimmed
        }

        [Fact]
        public async Task Handle_ShouldNotReject_WhenPlanBelongsToDifferentEnterprise()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
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

            var handler = new RejectPlanHandler(context, _currentUserServiceMock.Object, _loggerMock.Object);
            var command = new RejectPlanCommand { PlanId = planId, RejectionReason = "Test" };

            // Act & Assert
            await handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng.");
        }
    }
}
