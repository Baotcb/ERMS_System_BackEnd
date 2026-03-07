using ERMS.Application.Features.RecruitmentCampaigns.Queries.GetAllRecruitmentCampaigns;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Constants.Application;
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
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;
using OfferEntity = ERMS.Domain.Entities.Application.Offer;

namespace ERMS.UnitTests.Features.RecruitmentCampaigns.Queries.GetAllRecruitmentCampaigns
{
    public class GetAllRecruitmentCampaignsHandlerTest
    {
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        public GetAllRecruitmentCampaignsHandlerTest()
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

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery();

            // Act & Assert
            await handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào");
        }

        [Fact]
        public async Task Handle_ShouldReturnCampaigns_WithFiltersAndPagination()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Campaign One", 
                    CampaignCode = "C1", 
                    Status = "Active", 
                    CreatedById = userId, 
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Campaign Two", 
                    CampaignCode = "C2", 
                    Status = "Draft", 
                    CreatedById = userId, 
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = Guid.NewGuid(), 
                    CampaignName = "Other Ent Campaign", 
                    CampaignCode = "OC", 
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                }
            });
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery { Page = 1, PageSize = 10, Search = "One" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().CampaignName.Should().Be("Campaign One");
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
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "C1", 
                    CampaignCode = "C1", 
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "C2", 
                    CampaignCode = "C2", 
                    Status = "Draft", 
                    CreatedById = userId,
                    IsDeleted = false
                }
            });
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery { Status = "Draft" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().Status.Should().Be("Draft");
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
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            for (int i = 1; i <= 5; i++)
            {
                context.RecruitmentCampaigns.Add(new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = $"Campaign {i}", 
                    CampaignCode = $"C{i}", 
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i),
                    IsDeleted = false
                });
            }
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery { Page = 1, PageSize = 2 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.Items.First().CampaignName.Should().Be("Campaign 5"); // OrderByDescending
        }

        [Fact]
        public async Task Handle_ShouldExcludeDeletedCampaigns()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Active Campaign", 
                    CampaignCode = "AC", 
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Deleted Campaign", 
                    CampaignCode = "DC", 
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = true
                }
            });
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().CampaignName.Should().Be("Active Campaign");
        }

        [Fact]
        public async Task Handle_ShouldFilterByFiscalYear_WhenProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "2024 Campaign", 
                    CampaignCode = "C2024", 
                    FiscalYear = 2024,
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "2025 Campaign", 
                    CampaignCode = "C2025", 
                    FiscalYear = 2025,
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                }
            });
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery { FiscalYear = 2024 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().FiscalYear.Should().Be(2024);
        }

        [Fact]
        public async Task Handle_ShouldFilterByFiscalQuarter_WhenProvided()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Q1 Campaign", 
                    CampaignCode = "CQ1", 
                    FiscalQuarter = 1,
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Q2 Campaign", 
                    CampaignCode = "CQ2", 
                    FiscalQuarter = 2,
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                }
            });
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery { FiscalQuarter = 1 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().FiscalQuarter.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoMatchingCampaigns()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);
            var query = new GetAllRecruitmentCampaignsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }


        [Fact]
        public async Task Handle_ShouldCalculateActualCost_InMonthlyEquivalent_ForAcceptedOffers()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var creatorId = Guid.NewGuid();
            var candidateUserId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var planDetailId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            context.Users.AddRange(
                new User { Id = creatorId, FullName = "Creator", Email = "creator@test.com" },
                new User { Id = candidateUserId, FullName = "Candidate", Email = "candidate@test.com" });

            context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign",
                CampaignCode = "C1",
                Status = "Active",
                CreatedById = creatorId,
                IsDeleted = false
            });

            context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                PlanCode = "P1",
                PlanName = "Plan 1",
                CreatedById = creatorId,
                IsDeleted = false
            });

            context.PlanDetails.Add(new PlanDetail
            {
                Id = planDetailId,
                RecruitmentPlanId = planId,
                PositionTitle = "Developer",
                RequestedById = creatorId,
                IsDeleted = false
            });

            context.JobPostings.Add(new JobPosting
            {
                Id = jobPostingId,
                EnterpriseId = enterpriseId,
                PlanDetailId = planDetailId,
                DepartmentId = 1,
                JobTitle = "Developer",
                Description = "Desc",
                CreatedById = creatorId,
                IsDeleted = false
            });

            context.Candidates.Add(new CandidateEntity
            {
                Id = candidateId,
                UserId = candidateUserId,
                IsDeleted = false
            });

            var acceptedMonthlyApplicationId = Guid.NewGuid();
            var acceptedYearlyApplicationId = Guid.NewGuid();
            var rejectedApplicationId = Guid.NewGuid();
            var deletedOfferApplicationId = Guid.NewGuid();

            context.Applications.AddRange(
                new ApplicationEntity { Id = acceptedMonthlyApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false },
                new ApplicationEntity { Id = acceptedYearlyApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false },
                new ApplicationEntity { Id = rejectedApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false },
                new ApplicationEntity { Id = deletedOfferApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false });

            context.Offers.AddRange(
                new OfferEntity
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = acceptedMonthlyApplicationId,
                    Position = "Developer",
                    DepartmentId = 1,
                    Salary = 3000m,
                    SalaryFrequency = OfferSalaryFrequency.Monthly,
                    StartDate = DateTime.UtcNow.AddDays(7),
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    Status = OfferStatus.Accepted,
                    CreatedById = creatorId,
                    IsDeleted = false
                },
                new OfferEntity
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = acceptedYearlyApplicationId,
                    Position = "Developer",
                    DepartmentId = 1,
                    Salary = 12000m,
                    SalaryFrequency = OfferSalaryFrequency.Yearly,
                    StartDate = DateTime.UtcNow.AddDays(7),
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    Status = OfferStatus.Accepted,
                    CreatedById = creatorId,
                    IsDeleted = false
                },
                new OfferEntity
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = rejectedApplicationId,
                    Position = "Developer",
                    DepartmentId = 1,
                    Salary = 5000m,
                    SalaryFrequency = OfferSalaryFrequency.Monthly,
                    StartDate = DateTime.UtcNow.AddDays(7),
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    Status = OfferStatus.Rejected,
                    CreatedById = creatorId,
                    IsDeleted = false
                },
                new OfferEntity
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = deletedOfferApplicationId,
                    Position = "Developer",
                    DepartmentId = 1,
                    Salary = 9999m,
                    SalaryFrequency = OfferSalaryFrequency.Monthly,
                    StartDate = DateTime.UtcNow.AddDays(7),
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    Status = OfferStatus.Accepted,
                    CreatedById = creatorId,
                    IsDeleted = true
                });

            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);

            // Act
            var result = await handler.Handle(new GetAllRecruitmentCampaignsQuery(), CancellationToken.None);

            // Assert
            result.Items.Should().ContainSingle();
            result.Items[0].ActualCost.Should().Be(4000m);
        }
        [Fact]
        public async Task Handle_ShouldSearchInCampaignNameAndCode()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ERMSDbContext(options);

            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            context.Users.Add(user);

            context.RecruitmentCampaigns.AddRange(new List<RecruitmentCampaign>
            {
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Summer Hiring", 
                    CampaignCode = "SH2024", 
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                },
                new RecruitmentCampaign 
                { 
                    Id = Guid.NewGuid(), 
                    EnterpriseId = enterpriseId, 
                    CampaignName = "Winter Recruitment", 
                    CampaignCode = "WR2024", 
                    Status = "Active", 
                    CreatedById = userId,
                    IsDeleted = false
                }
            });
            await context.SaveChangesAsync();

            var handler = new GetAllRecruitmentCampaignsHandler(context, _currentUserServiceMock.Object);

            // Act - Search by name
            var resultByName = await handler.Handle(new GetAllRecruitmentCampaignsQuery { Search = "summer" }, CancellationToken.None);

            // Assert
            resultByName.Items.Should().HaveCount(1);
            resultByName.Items.First().CampaignName.Should().Be("Summer Hiring");

            // Act - Search by code
            var resultByCode = await handler.Handle(new GetAllRecruitmentCampaignsQuery { Search = "WR" }, CancellationToken.None);

            // Assert
            resultByCode.Items.Should().HaveCount(1);
            resultByCode.Items.First().CampaignCode.Should().Be("WR2024");
        }
    }
}

