using ERMS.Application.Features.JobPostings.Commands.DeleteJobPosting;
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

namespace ERMS.UnitTests.Features.JobPostings.Commands.DeleteJobPosting
{
    public class DeleteJobPostingHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<DeleteJobPostingHandler>> _loggerMock;
        private readonly DeleteJobPostingHandler _handler;

        public DeleteJobPostingHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<DeleteJobPostingHandler>>();

            _handler = new DeleteJobPostingHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotHRManager()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Candidate });
            var command = new DeleteJobPostingCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only HR Manager can delete job postings.");
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

            var command = new DeleteJobPostingCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"JobPosting with ID {command.Id} not found.");
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenPublishedJobHasApplications()
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
            _context.Applications.Add(new ERMS.Domain.Entities.Application.Application
            {
                Id = Guid.NewGuid(),
                JobPostingId = jobPostingId,
                CandidateId = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            var command = new DeleteJobPostingCommand { Id = jobPostingId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot delete a published job posting that has candidate applications. Please close the job posting instead.");
        }

        [Fact]
        public async Task Handle_ShouldSoftDelete_WhenSuccessful()
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

            var command = new DeleteJobPostingCommand { Id = jobPostingId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var deletedJob = await _context.JobPostings.FindAsync(jobPostingId);
            deletedJob.Should().NotBeNull();
            deletedJob!.IsDeleted.Should().BeTrue();
            deletedJob.DeletedAt.Should().NotBeNull();
        }
    }
}
