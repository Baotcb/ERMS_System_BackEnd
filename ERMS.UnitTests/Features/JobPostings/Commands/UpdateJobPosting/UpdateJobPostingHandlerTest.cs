using ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.JobPostings.Commands.UpdateJobPosting
{
    public class UpdateJobPostingHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<UpdateJobPostingHandler>> _loggerMock;
        private readonly UpdateJobPostingHandler _handler;

        public UpdateJobPostingHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<UpdateJobPostingHandler>>();

            _handler = new UpdateJobPostingHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDeadlineInPast()
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
                JobTitle = "Job",
                Description = "Desc",
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new UpdateJobPostingCommand 
            { 
                Id = jobPostingId, 
                ApplicationDeadline = DateTime.UtcNow.AddDays(-1) 
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Hạn nộp hồ sơ phải trong tương lai.");
        }

        [Fact]
        public async Task Handle_ShouldUpdateFields_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();
            var newDeadline = DateTime.UtcNow.AddMonths(1);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.JobPostings.Add(new JobPosting
            {
                Id = jobPostingId,
                EnterpriseId = enterpriseId,
                JobTitle = "Old Title",
                Description = "Old Desc",
                Benefits = "Old Benefits",
                Location = "Old Location",
                RemoteOption = "Old Remote",
                CreatedById = userId
            });
            await _context.SaveChangesAsync();

            var command = new UpdateJobPostingCommand 
            { 
                Id = jobPostingId,
                Description = "New Desc",
                Benefits = "New Benefits",
                ApplicationDeadline = newDeadline,
                Location = "New Location",
                RemoteOption = "New Remote"
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedJob = await _context.JobPostings.FindAsync(jobPostingId);
            updatedJob.Should().NotBeNull();
            updatedJob!.Description.Should().Be("New Desc");
            updatedJob.Benefits.Should().Be("New Benefits");
            updatedJob.ApplicationDeadline.Should().Be(newDeadline);
            updatedJob.Location.Should().Be("New Location");
            updatedJob.RemoteOption.Should().Be("New Remote");
        }
    }
}
