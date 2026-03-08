using ERMS.Application.Features.RecruitmentCampaigns.Queries.GetRecruitmentCampaignById;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using CandidateEntity = ERMS.Domain.Entities.Candidate.Candidate;
using OfferEntity = ERMS.Domain.Entities.Application.Offer;

namespace ERMS.UnitTests.Features.RecruitmentCampaigns.Queries.GetRecruitmentCampaignById
{
    public class GetRecruitmentCampaignByIdHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetRecruitmentCampaignByIdHandler _handler;

        public GetRecruitmentCampaignByIdHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new GetRecruitmentCampaignByIdHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCampaignNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
            var query = new GetRecruitmentCampaignByIdQuery { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy chiến dịch tuyển dụng");
        }

        [Fact]
        public async Task Handle_ShouldReturnDetails_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Creator" };
            _context.Users.Add(user);

            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Target Campaign",
                CampaignCode = "TC",
                Status = "Draft",
                CreatedById = userId,
                FiscalYear = 2024
            });

            _context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                PlanName = "Plan 1",
                PlanCode = "P1",
                CreatedById = userId
            });

            await _context.SaveChangesAsync();

            var query = new GetRecruitmentCampaignByIdQuery { Id = campaignId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.CampaignName.Should().Be("Target Campaign");
            result.CreatedByName.Should().Be("Creator");
            result.TotalPlansCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ShouldCalculateActualCost_InMonthlyEquivalent_ForAcceptedOffers()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var candidateUserId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var planDetailId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Users.AddRange(
                new User { Id = creatorId, FullName = "Creator", Email = "creator@test.com" },
                new User { Id = candidateUserId, FullName = "Candidate", Email = "candidate@test.com" });

            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Target Campaign",
                CampaignCode = "TC",
                Status = "Draft",
                CreatedById = creatorId,
                FiscalYear = 2024,
                IsDeleted = false
            });

            _context.RecruitmentPlans.Add(new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaignId,
                PlanName = "Plan 1",
                PlanCode = "P1",
                CreatedById = creatorId,
                IsDeleted = false
            });

            _context.PlanDetails.Add(new PlanDetail
            {
                Id = planDetailId,
                RecruitmentPlanId = planId,
                PositionTitle = "Developer",
                RequestedById = creatorId,
                IsDeleted = false
            });

            _context.JobPostings.Add(new JobPosting
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

            _context.Candidates.Add(new CandidateEntity
            {
                Id = candidateId,
                UserId = candidateUserId,
                IsDeleted = false
            });

            var acceptedMonthlyApplicationId = Guid.NewGuid();
            var acceptedYearlyApplicationId = Guid.NewGuid();
            var rejectedApplicationId = Guid.NewGuid();
            var deletedOfferApplicationId = Guid.NewGuid();

            _context.Applications.AddRange(
                new ApplicationEntity { Id = acceptedMonthlyApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false },
                new ApplicationEntity { Id = acceptedYearlyApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false },
                new ApplicationEntity { Id = rejectedApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false },
                new ApplicationEntity { Id = deletedOfferApplicationId, JobPostingId = jobPostingId, CandidateId = candidateId, IsDeleted = false });

            _context.Offers.AddRange(
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

            await _context.SaveChangesAsync();

            var query = new GetRecruitmentCampaignByIdQuery { Id = campaignId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.ActualCost.Should().Be(4000m);
        }
    }
}
