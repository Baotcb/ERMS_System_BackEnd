using ERMS.Application.Features.RecruitmentCampaigns.Queries.GetRecruitmentCampaignById;
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

            var campaign = new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                CampaignName = "Target Campaign",
                CampaignCode = "TC",
                Status = "Draft",
                CreatedById = userId,
                FiscalYear = 2024
            };
            _context.RecruitmentCampaigns.Add(campaign);

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
    }
}
