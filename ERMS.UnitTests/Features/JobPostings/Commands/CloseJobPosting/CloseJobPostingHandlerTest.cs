using ERMS.Application.Features.JobPostings.Commands.CloseJobPosting;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
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

namespace ERMS.UnitTests.Features.JobPostings.Commands.CloseJobPosting
{
    public class CloseJobPostingHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CloseJobPostingHandler>> _loggerMock;
        private readonly CloseJobPostingHandler _handler;

        public CloseJobPostingHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CloseJobPostingHandler>>();

            _handler = new CloseJobPostingHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new CloseJobPostingCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng chưa được xác thực.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotHRManager()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Candidate });
            var command = new CloseJobPostingCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ HR Manager mới có quyền đóng tin tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenJobPostingNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var command = new CloseJobPostingCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Không tìm thấy tin tuyển dụng với ID {command.Id}.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenStatusIsNotPublished()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.JobPostings.Add(new JobPosting
            {
                Id = jobPostingId,
                EnterpriseId = enterpriseId,
                JobTitle = "Test Job",
                Description = "Description",
                Status = JobPostingStatus.Draft,
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new CloseJobPostingCommand { Id = jobPostingId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Không thể đóng tin. Trạng thái hiện tại '{JobPostingStatus.Draft}' phải là 'Published'.");
        }

        [Fact]
        public async Task Handle_ShouldUpdateStatusToClosed_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.JobPostings.Add(new JobPosting
            {
                Id = jobPostingId,
                EnterpriseId = enterpriseId,
                JobTitle = "Test Job",
                Description = "Description",
                Status = JobPostingStatus.Published,
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new CloseJobPostingCommand { Id = jobPostingId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedJob = await _context.JobPostings.FindAsync(jobPostingId);
            updatedJob.Should().NotBeNull();
            updatedJob!.Status.Should().Be(JobPostingStatus.Closed);
            updatedJob.ClosedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
