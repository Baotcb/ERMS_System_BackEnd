using ERMS.Domain.Entities.Organization;
using ERMS.Application.Features.RecruitmentPlans.Queries.GetAllRecruitmentPlans;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Identity;
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
using ERMS.Domain.Entities.Organization;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Queries.GetAllRecruitmentPlans
{
    public class GetAllRecruitmentPlansHandlerTest
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        public GetAllRecruitmentPlansHandlerTest()
        {
            _currentUserServiceMock = new Mock<ICurrentUserService>();
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery();

            // Act & Assert
            await handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào");
        }

        [Fact]
        public async Task Handle_ShouldReturnPlans_WithFiltersAndPagination()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            var campaign = new RecruitmentCampaign 
            { 
                Id = campaignId, 
                CampaignName = "Camp 1", 
                EnterpriseId = enterpriseId, 
                CreatedById = userId, 
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.AddRange(new List<RecruitmentPlan>
            {
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "Plan One", 
                    PlanCode = "P1", 
                    Status = PlanStatus.Approved, 
                    CreatedById = userId, 
                    CampaignId = campaignId, 
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    IsDeleted = false
                },
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "Plan Two", 
                    PlanCode = "P2", 
                    Status = PlanStatus.Draft, 
                    CreatedById = userId, 
                    CampaignId = campaignId, 
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    IsDeleted = false
                },
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = Guid.NewGuid(), DepartmentId = 1, 
                    PlanName = "Other Ent Plan", 
                    PlanCode = "OP", 
                    Status = PlanStatus.Approved, 
                    CreatedById = userId, 
                    CampaignId = Guid.NewGuid(),
                    IsDeleted = false
                }
            });
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery { Page = 1, PageSize = 10, Search = "One" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().PlanName.Should().Be("Plan One");
            result.Items.First().CampaignName.Should().Be("Camp 1");
            result.Items.First().CreatedByName.Should().Be("Test User");
        }

        [Fact]
        public async Task Handle_ShouldFilterByStatus_WhenProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            var campaign = new RecruitmentCampaign 
            { 
                Id = campaignId, 
                CampaignName = "C1", 
                EnterpriseId = enterpriseId, 
                CreatedById = userId, 
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.AddRange(new List<RecruitmentPlan>
            {
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "P1", 
                    PlanCode = "P1", 
                    Status = PlanStatus.Approved, 
                    CreatedById = userId, 
                    CampaignId = campaignId,
                    IsDeleted = false
                },
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "P2", 
                    PlanCode = "P2", 
                    Status = PlanStatus.Draft, 
                    CreatedById = userId, 
                    CampaignId = campaignId,
                    IsDeleted = false
                }
            });
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery { Status = PlanStatus.Draft };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Status.Should().Be(PlanStatus.Draft);
        }

        [Fact]
        public async Task Handle_ShouldReturnPagedResults_WhenRequested()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            var campaign = new RecruitmentCampaign 
            { 
                Id = campaignId, 
                CampaignName = "Campaign", 
                EnterpriseId = enterpriseId, 
                CreatedById = userId, 
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            for (int i = 1; i <= 5; i++)
            {
                context.RecruitmentPlans.Add(new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = $"Plan {i}", 
                    PlanCode = $"P{i}",
                    Status = PlanStatus.Draft,
                    CreatedById = userId,
                    CampaignId = campaignId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i),
                    IsDeleted = false
                });
            }
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery { Page = 1, PageSize = 2 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.Items.First().PlanName.Should().Be("Plan 5"); // OrderByDescending
        }

        [Fact]
        public async Task Handle_ShouldExcludeDeletedPlans()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            var campaign = new RecruitmentCampaign 
            { 
                Id = campaignId, 
                CampaignName = "Campaign", 
                EnterpriseId = enterpriseId, 
                CreatedById = userId, 
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.AddRange(new List<RecruitmentPlan>
            {
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "Active Plan", 
                    PlanCode = "AP",
                    Status = PlanStatus.Draft,
                    CreatedById = userId,
                    CampaignId = campaignId,
                    IsDeleted = false
                },
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "Deleted Plan", 
                    PlanCode = "DP",
                    Status = PlanStatus.Draft,
                    CreatedById = userId,
                    CampaignId = campaignId,
                    IsDeleted = true
                }
            });
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().PlanName.Should().Be("Active Plan");
        }

        [Fact]
        public async Task Handle_ShouldSearchInPlanNameAndCode()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            var campaign = new RecruitmentCampaign 
            { 
                Id = campaignId, 
                CampaignName = "Campaign", 
                EnterpriseId = enterpriseId, 
                CreatedById = userId, 
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.AddRange(new List<RecruitmentPlan>
            {
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "Q1 Hiring Plan", 
                    PlanCode = "Q1H2024",
                    Status = PlanStatus.Draft,
                    CreatedById = userId,
                    CampaignId = campaignId,
                    IsDeleted = false
                },
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, DepartmentId = 1, 
                    PlanName = "Q2 Recruitment", 
                    PlanCode = "Q2R2024",
                    Status = PlanStatus.Draft,
                    CreatedById = userId,
                    CampaignId = campaignId,
                    IsDeleted = false
                }
            });
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);

            // Act - Search by name
            var resultByName = await handler.Handle(new GetAllRecruitmentPlansQuery { Search = "hiring" }, CancellationToken.None);

            // Assert
            resultByName.Items.Should().HaveCount(1);
            resultByName.Items.First().PlanName.Should().Be("Q1 Hiring Plan");

            // Act - Search by code
            var resultByCode = await handler.Handle(new GetAllRecruitmentPlansQuery { Search = "Q2R" }, CancellationToken.None);

            // Assert
            resultByCode.Items.Should().HaveCount(1);
            resultByCode.Items.First().PlanCode.Should().Be("Q2R2024");
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoMatchingPlans()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ShouldIncludeApproverInfo_WhenPlanIsApproved()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var creator = new User { Id = creatorId, FullName = "Plan Creator", Email = "creator@test.com" };
            var approver = new User { Id = approverId, FullName = "Plan Approver", Email = "approver@test.com" };
            context.Users.AddRange(creator, approver);

            var campaign = new RecruitmentCampaign 
            { 
                Id = campaignId, 
                CampaignName = "Campaign", 
                EnterpriseId = enterpriseId, 
                CreatedById = creatorId, 
                CampaignCode = "C1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.Add(campaign);

            context.RecruitmentPlans.Add(new RecruitmentPlan 
            { 
                Id = Guid.NewGuid(), 
                EnterpriseId = enterpriseId, DepartmentId = 1, 
                PlanName = "Approved Plan", 
                PlanCode = "AP",
                Status = PlanStatus.Approved,
                CreatedById = creatorId,
                ApprovedById = approverId,
                ApprovedAt = DateTime.UtcNow.AddDays(-1),
                CampaignId = campaignId,
                IsDeleted = false
            });
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().CreatedByName.Should().Be("Plan Creator");
            result.Items.First().ApprovedByName.Should().Be("Plan Approver");
            result.Items.First().ApprovedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldOnlyReturnPlansFromUserEnterprise()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var userEnterpriseId = Guid.NewGuid();
            var otherEnterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(userEnterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            var userCampaign = new RecruitmentCampaign 
            { 
                Id = Guid.NewGuid(), 
                CampaignName = "User Campaign", 
                EnterpriseId = userEnterpriseId, 
                CreatedById = userId, 
                CampaignCode = "UC1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };

            var otherCampaign = new RecruitmentCampaign 
            { 
                Id = Guid.NewGuid(), 
                CampaignName = "Other Campaign", 
                EnterpriseId = otherEnterpriseId, 
                CreatedById = Guid.NewGuid(), 
                CampaignCode = "OC1",
                Status = CampaignStatus.Open,
                IsDeleted = false
            };
            context.RecruitmentCampaigns.AddRange(userCampaign, otherCampaign);

            context.RecruitmentPlans.AddRange(new List<RecruitmentPlan>
            {
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = userEnterpriseId, DepartmentId = 1, 
                    PlanName = "User Plan", 
                    PlanCode = "UP",
                    Status = PlanStatus.Draft,
                    CreatedById = userId,
                    CampaignId = userCampaign.Id,
                    IsDeleted = false
                },
                new RecruitmentPlan 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = otherEnterpriseId, DepartmentId = 1, 
                    PlanName = "Other Plan", 
                    PlanCode = "OP",
                    Status = PlanStatus.Draft,
                    CreatedById = Guid.NewGuid(),
                    CampaignId = otherCampaign.Id,
                    IsDeleted = false
                }
            });
            context.Departments.Add(new Department { Id = 1, DepartmentName = "HR" }); await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentPlansHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentPlansQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().PlanName.Should().Be("User Plan");
        }
    }
}




