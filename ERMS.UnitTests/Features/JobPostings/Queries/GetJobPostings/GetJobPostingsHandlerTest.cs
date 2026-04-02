using ERMS.Application.Features.JobPostings.Queries.GetJobPostings;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Organization;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.JobPostings.Queries.GetJobPostings
{
    public class GetJobPostingsHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetJobPostingsHandler _handler;

        public GetJobPostingsHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetJobPostingsHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotHRManagerOrDirector()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Candidate });
            var query = new GetJobPostingsQuery();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ HR Manager hoặc Giám đốc mới có quyền xem tin tuyển dụng.");
        }

        [Fact]
        public async Task Handle_ShouldReturnPagedList_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var departmentId = 1;

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var department = new Department { Id = departmentId, DepartmentName = "IT" };
            _context.Departments.Add(department);

            _context.JobPostings.AddRange(new List<JobPosting>
            {
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, JobTitle = "Job 1", DepartmentId = departmentId, Description = "Desc", CreatedById = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, JobTitle = "Job 2", DepartmentId = departmentId, Description = "Desc", CreatedById = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = Guid.NewGuid(), JobTitle = "Other Job", DepartmentId = departmentId, Description = "Desc", CreatedById = userId } // Different enterprise
            });
            await _context.SaveChangesAsync();

            var query = new GetJobPostingsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.First().JobTitle.Should().Be("Job 2"); // Descending order
        }
    }
}
