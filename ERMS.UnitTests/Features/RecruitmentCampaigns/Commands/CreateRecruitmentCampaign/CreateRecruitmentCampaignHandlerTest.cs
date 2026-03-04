using ERMS.Application.Features.RecruitmentCampaigns.Commands.CreateRecruitmentCampaign;
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

namespace ERMS.UnitTests.Features.RecruitmentCampaigns.Commands.CreateRecruitmentCampaign
{
    public class CreateRecruitmentCampaignHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly CreateRecruitmentCampaignHandler _handler;

        public CreateRecruitmentCampaignHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new CreateRecruitmentCampaignHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new CreateRecruitmentCampaignCommand();

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
            var command = new CreateRecruitmentCampaignCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ HR Manager mới có quyền tạo chiến dịch tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new CreateRecruitmentCampaignCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDatesAreInvalid()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new CreateRecruitmentCampaignCommand
            {
                SubmissionStartDate = DateTime.UtcNow.AddDays(10),
                SubmissionEndDate = DateTime.UtcNow.AddDays(5)
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenTargetHireDatesAreInvalid()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new CreateRecruitmentCampaignCommand
            {
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddDays(10),
                TargetHireStartDate = DateTime.UtcNow.AddDays(20),
                TargetHireEndDate = DateTime.UtcNow.AddDays(15)
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Ngày kết thúc tuyển dụng phải sau ngày bắt đầu tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenFiscalYearIsInvalid()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new CreateRecruitmentCampaignCommand
            {
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddDays(10),
                FiscalYear = 2019
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Năm tài chính phải từ 2020 trở đi.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenFiscalQuarterIsInvalid()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());

            var command = new CreateRecruitmentCampaignCommand
            {
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddDays(10),
                FiscalYear = 2024,
                FiscalQuarter = 5
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Quý tài chính phải từ 1 đến 4.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCampaignCodeExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.RecruitmentCampaigns.Add(new RecruitmentCampaign
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CampaignCode = "EXISTING",
                CampaignName = "Existing",
                FiscalYear = 2024,
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddDays(30),
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new CreateRecruitmentCampaignCommand
            {
                CampaignCode = "EXISTING",
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddDays(10),
                FiscalYear = 2024
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Mã chiến dịch 'EXISTING' đã tồn tại trong doanh nghiệp.");
        }

        [Fact]
        public async Task Handle_ShouldCreateCampaign_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var command = new CreateRecruitmentCampaignCommand
            {
                CampaignName = "New Campaign",
                CampaignCode = "NEW-CODE",
                FiscalYear = 2024,
                FiscalQuarter = 1,
                SubmissionStartDate = DateTime.UtcNow,
                SubmissionEndDate = DateTime.UtcNow.AddDays(30),
                Description = "Test description",
                MaxTotalPositions = 10,
                TotalBudgetCeiling = 1000000
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            var campaign = await _context.RecruitmentCampaigns.FindAsync(result);
            campaign.Should().NotBeNull();
            campaign!.CampaignName.Should().Be(command.CampaignName);
            campaign.CampaignCode.Should().Be(command.CampaignCode);
            campaign.Status.Should().Be(CampaignStatus.Draft);
            campaign.EnterpriseId.Should().Be(enterpriseId);
            campaign.CreatedById.Should().Be(userId);
        }
    }
}
