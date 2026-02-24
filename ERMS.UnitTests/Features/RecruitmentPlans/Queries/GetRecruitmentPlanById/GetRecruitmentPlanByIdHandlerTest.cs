using ERMS.Application.Features.RecruitmentPlans.Queries.GetRecruitmentPlanById;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Identity;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Queries.GetRecruitmentPlanById
{
    public class GetRecruitmentPlanByIdHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetRecruitmentPlanByIdHandler _handler;

        public GetRecruitmentPlanByIdHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetRecruitmentPlanByIdHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
            var query = new GetRecruitmentPlanByIdQuery { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng");
        }

        [Fact]
        public async Task Handle_ShouldReturnDetails_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Creator" };
            _context.Users.Add(user);

            var campaign = new RecruitmentCampaign { Id = Guid.NewGuid(), CampaignName = "Target Campaign", EnterpriseId = enterpriseId, CreatedById = userId, CampaignCode = "C1" };
            _context.RecruitmentCampaigns.Add(campaign);

            var plan = new RecruitmentPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                CampaignId = campaign.Id,
                PlanName = "Target Plan",
                PlanCode = "TP",
                Status = "Draft",
                CreatedById = userId
            };
            _context.RecruitmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            var query = new GetRecruitmentPlanByIdQuery { Id = planId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.PlanName.Should().Be("Target Plan");
            result.CampaignName.Should().Be("Target Campaign");
            result.CreatedByName.Should().Be("Creator");
        }
    }
}
