using ERMS.Application.Features.RecruitmentCampaigns.Commands.UpdateCampaignStatus;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentCampaigns.Commands.UpdateCampaignStatus
{
    public class UpdateCampaignStatusHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly UpdateCampaignStatusHandler _handler;

        public UpdateCampaignStatusHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new UpdateCampaignStatusHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new UpdateCampaignStatusCommand { Id = Guid.NewGuid(), NewStatus = CampaignStatus.Draft };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Không tìm thấy thông tin người dùng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotHRManager()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Candidate });
            var command = new UpdateCampaignStatusCommand { Id = Guid.NewGuid(), NewStatus = CampaignStatus.Draft };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ HR Manager mới có quyền thay đổi trạng thái chiến dịch.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new UpdateCampaignStatusCommand { Id = Guid.NewGuid(), NewStatus = CampaignStatus.Draft };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenStatusIsInvalid()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new UpdateCampaignStatusCommand { Id = Guid.NewGuid(), NewStatus = "INVALID_STATUS" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Trạng thái 'INVALID_STATUS' không hợp lệ*");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCampaignNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new UpdateCampaignStatusCommand { Id = Guid.NewGuid(), NewStatus = CampaignStatus.Draft };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy chiến dịch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenTransitionIsInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                Status = CampaignStatus.Draft,
                CampaignCode = "C1",
                CampaignName = "Campaign 1",
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            // Transition directly to Closed from Draft (invalid transition)
            var command = new UpdateCampaignStatusCommand { Id = campaignId, NewStatus = CampaignStatus.Closed };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Không thể chuyển từ trạng thái '{CampaignStatus.Draft}' sang '{CampaignStatus.Closed}'.");
        }

        [Fact]
        public async Task Handle_ShouldUpdateStatus_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var campaignId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = campaignId,
                EnterpriseId = enterpriseId,
                Status = CampaignStatus.Draft,
                CampaignCode = "C1",
                CampaignName = "Campaign 1",
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new UpdateCampaignStatusCommand { Id = campaignId, NewStatus = CampaignStatus.Open };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            var campaign = await _context.RecruitmentCampaigns.FindAsync(campaignId);
            campaign.Should().NotBeNull();
            campaign!.Status.Should().Be(CampaignStatus.Open);
        }
    }
}
