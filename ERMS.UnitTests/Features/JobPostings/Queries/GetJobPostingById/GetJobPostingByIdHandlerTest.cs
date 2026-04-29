using ERMS.Application.Features.JobPostings.Queries.GetJobPostingById;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Application;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ERMS.Domain.Entities.Identity;

namespace ERMS.UnitTests.Features.JobPostings.Queries.GetJobPostingById
{
    public class GetJobPostingByIdHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetJobPostingByIdHandler _handler;

        public GetJobPostingByIdHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetJobPostingByIdHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenJobPostingNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var query = new GetJobPostingByIdQuery { Id = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldReturnDetails_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var departmentId = 1;
            var jobPostingId = Guid.NewGuid();
            var planDetailId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Test User", Email = "test@user.com" };
            _context.Users.Add(user);

            var department = new Department { Id = departmentId, DepartmentName = "HR" };
            _context.Departments.Add(department);

            var campaign = new RecruitmentCampaign
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CampaignName = "Campaign 1",
                CampaignCode = "C1",
                FiscalYear = 2024,
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddMonths(1),
                CreatedById = userId
            };
            _context.RecruitmentCampaigns.Add(campaign);

            var plan = new RecruitmentPlan 
            { 
                Id = Guid.NewGuid(), 
                PlanName = "Plan 1", 
                PlanCode = "P1",
                EnterpriseId = enterpriseId, 
                CampaignId = campaign.Id,
                DepartmentId = departmentId,
                CreatedById = userId
            };
            _context.RecruitmentPlans.Add(plan);

            var planDetail = new PlanDetail 
            { 
                Id = planDetailId, 
                RecruitmentPlanId = plan.Id, 
                RequestedById = userId,
                PositionTitle = "Software Engineer",
                Quantity = 5 
            };
            _context.PlanDetails.Add(planDetail);

            var jobPosting = new JobPosting
            {
                Id = jobPostingId,
                EnterpriseId = enterpriseId,
                JobTitle = "Job Detail",
                JobCode = "J1",
                DepartmentId = departmentId,
                Description = "Desc",
                CreatedById = userId,
                PlanDetailId = planDetailId
            };
            _context.JobPostings.Add(jobPosting);

            _context.Applications.Add(new ERMS.Domain.Entities.Application.Application
            {
                Id = Guid.NewGuid(),
                JobPostingId = jobPostingId,
                CandidateId = Guid.NewGuid(),
                Stage = ApplicationStage.Hired
            });
            await _context.SaveChangesAsync();

            var query = new GetJobPostingByIdQuery { Id = jobPostingId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.JobTitle.Should().Be("Job Detail");
            result.QuotaUsed.Should().Be(1);
            result.QuotaTotal.Should().Be(5);
        }
    }
}
