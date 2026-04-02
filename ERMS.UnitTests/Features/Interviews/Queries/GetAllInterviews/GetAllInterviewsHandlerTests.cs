using ERMS.Application.Features.Interviews.Queries.GetAllInterviews;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Enums;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Interviews.Queries.GetAllInterviews;

public class GetAllInterviewsHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly GetAllInterviewsHandler _handler;

    public GetAllInterviewsHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new GetAllInterviewsHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    private void SetupMockContext(List<Interview> interviews)
    {
        var dbSetMock = interviews.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Interviews).Returns(dbSetMock.Object);
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        var query = new GetAllInterviewsQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserWithoutHRRole_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
        _currentUserServiceMock.Setup(s => s.Roles).Returns(new List<string> { AppRoles.Employee });

        var query = new GetAllInterviewsQuery();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(query, CancellationToken.None));
        
        ex.Message.Should().Be("Chỉ HR Manager hoặc Giám đốc mới có quyền xem tất cả buổi phỏng vấn.");
    }

    [Fact]
    public async Task Handle_UserWithoutEnterpriseId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(s => s.Roles).Returns(new List<string> { AppRoles.HRManager });
        _currentUserServiceMock.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);

        var query = new GetAllInterviewsQuery();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(query, CancellationToken.None));
            
        ex.Message.Should().Be("Người dùng không thuộc doanh nghiệp nào.");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsScoupedInterviews()
    {
        // Arrange
        var enterpriseId1 = Guid.NewGuid();
        var enterpriseId2 = Guid.NewGuid();
        
        _currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(s => s.Roles).Returns(new List<string> { AppRoles.HRManager });
        _currentUserServiceMock.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId1);

        var job1 = new JobPosting { EnterpriseId = enterpriseId1, JobTitle = "Job 1" };
        var job2 = new JobPosting { EnterpriseId = enterpriseId2, JobTitle = "Job 2" };

        var user1 = new User { Id = Guid.NewGuid(), FullName = "Candidate 1", Email = "c1@test.com" };
        var candidate1 = new Candidate { User = user1 };
        
        var user2 = new User { Id = Guid.NewGuid(), FullName = "Candidate 2", Email = "c2@test.com" };
        var candidate2 = new Candidate { User = user2 };

        var app1 = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job1, Candidate = candidate1, IsDeleted = false };
        var app2 = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job2, Candidate = candidate2, IsDeleted = false };

        var interviews = new List<Interview>
        {
            new Interview { Id = Guid.NewGuid(), Application = app1, ApplicationId = app1.Id, IsDeleted = false, ScheduledAt = DateTime.UtcNow.AddDays(1) },
            new Interview { Id = Guid.NewGuid(), Application = app1, ApplicationId = app1.Id, IsDeleted = false, ScheduledAt = DateTime.UtcNow.AddDays(2) },
            new Interview { Id = Guid.NewGuid(), Application = app2, ApplicationId = app2.Id, IsDeleted = false } // Belong to different enterprise
        };

        SetupMockContext(interviews);

        var query = new GetAllInterviewsQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TotalCount.Should().Be(2); // Only 2 match enterpriseId1
        response.Items.Should().HaveCount(2);
        response.Items.All(i => i.JobTitle == "Job 1").Should().BeTrue();
        
        // Check sorting: Most recent next
        response.Items[0].ScheduledAt.Should().BeAfter(response.Items[1].ScheduledAt.Value);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        
        _currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(s => s.Roles).Returns(new List<string> { AppRoles.Director });
        _currentUserServiceMock.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

        var job = new JobPosting { EnterpriseId = enterpriseId, JobTitle = "Job" };
        var user = new User { Id = Guid.NewGuid(), FullName = "C", Email = "c@t.com" };
        var candidate = new Candidate { User = user };
        var app = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job, Candidate = candidate, IsDeleted = false };

        var interviews = new List<Interview>
        {
            new Interview { Id = Guid.NewGuid(), Application = app, Status = "Scheduled", IsDeleted = false },
            new Interview { Id = Guid.NewGuid(), Application = app, Status = "Completed", IsDeleted = false }
        };

        SetupMockContext(interviews);

        var query = new GetAllInterviewsQuery { StatusFilter = "Completed" };

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TotalCount.Should().Be(1);
        response.Items.First().Status.Should().Be("Completed");
    }
}
