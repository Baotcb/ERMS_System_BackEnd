using ERMS.Application.Features.JobPostings.Commands.SaveJobPosting;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.JobPostings.Commands.SaveJobPosting
{
    public class SaveJobPostingHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly SaveJobPostingHandler _handler;

        public SaveJobPostingHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new SaveJobPostingHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenCandidateNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            var command = new SaveJobPostingCommand { JobPostingId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Không tìm thấy hồ sơ ứng viên");
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenJobPostingNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            _context.Candidates.Add(new Candidate { Id = candidateId, UserId = userId });
            await _context.SaveChangesAsync();

            var command = new SaveJobPostingCommand { JobPostingId = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Không tìm thấy tin tuyển dụng");
        }

        [Fact]
        public async Task Handle_ShouldReturnExistingId_WhenJobAlreadySaved()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();
            var savedJobId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            _context.Candidates.Add(new Candidate { Id = candidateId, UserId = userId });
            _context.JobPostings.Add(new JobPosting 
            { 
                Id = jobPostingId, 
                JobTitle = "Job", 
                Description = "Desc",
                EnterpriseId = Guid.NewGuid(),
                DepartmentId = 1,
                CreatedById = Guid.NewGuid()
            });
            _context.SavedJobs.Add(new SavedJob 
            { 
                Id = savedJobId, 
                CandidateId = candidateId, 
                JobPostingId = jobPostingId 
            });
            await _context.SaveChangesAsync();

            var command = new SaveJobPostingCommand { JobPostingId = jobPostingId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(savedJobId);
        }

        [Fact]
        public async Task Handle_ShouldCreateNewSavedJob_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var jobPostingId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            _context.Candidates.Add(new Candidate { Id = candidateId, UserId = userId });
            _context.JobPostings.Add(new JobPosting 
            { 
                Id = jobPostingId, 
                JobTitle = "Job", 
                Description = "Desc",
                EnterpriseId = Guid.NewGuid(),
                DepartmentId = 1,
                CreatedById = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            var command = new SaveJobPostingCommand { JobPostingId = jobPostingId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            var savedJob = await _context.SavedJobs.FirstOrDefaultAsync(x => x.CandidateId == candidateId && x.JobPostingId == jobPostingId);
            savedJob.Should().NotBeNull();
            savedJob!.Id.Should().Be(result);
        }
    }
}
