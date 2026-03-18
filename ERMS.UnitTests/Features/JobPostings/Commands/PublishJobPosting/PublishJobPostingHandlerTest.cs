using ERMS.Application.Features.JobPostings.Commands.PublishJobPosting;
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.JobPostings.Commands.PublishJobPosting
{
    public class PublishJobPostingHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<PublishJobPostingHandler>> _loggerMock;
        private readonly PublishJobPostingHandler _handler;

        public PublishJobPostingHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<PublishJobPostingHandler>>();

            _handler = new PublishJobPostingHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenStatusIsNotDraft()
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
                JobTitle = "Published Job",
                Description = "Description",
                Status = JobPostingStatus.Published,
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new PublishJobPostingCommand { Id = jobPostingId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Không thể đăng tin. Trạng thái hiện tại '{JobPostingStatus.Published}' phải là 'Draft'.");
        }

        [Fact]
        public async Task Handle_ShouldPublish_WhenSuccessful()
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
                JobTitle = "Draft Job",
                Description = "Description",
                Status = JobPostingStatus.Draft,
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new PublishJobPostingCommand { Id = jobPostingId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var publishedJob = await _context.JobPostings.FindAsync(jobPostingId);
            publishedJob.Should().NotBeNull();
            publishedJob!.Status.Should().Be(JobPostingStatus.Published);
            publishedJob.PublishedAt.Should().NotBeNull();
            publishedJob.PublishedById.Should().Be(userId);
        }
    }
}
