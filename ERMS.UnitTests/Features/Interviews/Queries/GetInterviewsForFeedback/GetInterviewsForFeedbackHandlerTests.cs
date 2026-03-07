using ERMS.Application.Features.Interviews.Queries.GetInterviewsForFeedback;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Interviews.Queries.GetInterviewsForFeedback;

public class GetInterviewsForFeedbackHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetInterviewsForFeedbackHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly int _departmentId = 1;

    public GetInterviewsForFeedbackHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new GetInterviewsForFeedbackHandler(_mockContext.Object, _mockCurrentUserService.Object);
    }

    private void SetupAuthenticatedDepartmentHead()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
        _mockCurrentUserService.Setup(s => s.GetDepartmentIdAsync()).ReturnsAsync(_departmentId);
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
        var act = async () => await _handler.Handle(new GetInterviewsForFeedbackQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_UserWithoutDepartmentHeadRole_ThrowsUnauthorizedAccessException()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns(new List<string> { AppRoles.Employee });
        var act = async () => await _handler.Handle(new GetInterviewsForFeedbackQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Chỉ Trưởng phòng mới có quyền xem tổng quan đánh giá phỏng vấn.");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsScoupedInterviews()
    {
        // Arrange
        SetupAuthenticatedDepartmentHead();

        // Target matching interview (Completed, enterprise/dept match, has feedback)
        var user1 = new User { Id = Guid.NewGuid(), FullName = "Candidate 1", Email = "c1@test.com" };
        var candidate1 = new Candidate { User = user1 };
        var job1 = new JobPosting { EnterpriseId = _enterpriseId, DepartmentId = _departmentId, JobTitle = "Job 1" };
        var app1 = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job1, Candidate = candidate1, IsDeleted = false };

        var interview1 = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            Application = app1,
            Status = InterviewStatus.Scheduled,
            CompletedAt = DateTime.UtcNow.AddHours(-1),
            RoundNumber = 1,
            InterviewType = "HR",
            IsDeleted = false,
            Participants = new List<InterviewParticipant>
            {
                new InterviewParticipant { FeedbackSubmittedAt = DateTime.UtcNow, Role = "Interviewer" }
            }
        };

        // Out of scope department
        var jobOtherDept = new JobPosting { EnterpriseId = _enterpriseId, DepartmentId = 999, JobTitle = "Job 2" };
        var appOtherDept = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = jobOtherDept, Candidate = candidate1, IsDeleted = false };
        var interviewOtherDept = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = appOtherDept.Id,
            Application = appOtherDept,
            Status = InterviewStatus.Scheduled,
            IsDeleted = false,
            Participants = new List<InterviewParticipant>
            {
                new InterviewParticipant { FeedbackSubmittedAt = DateTime.UtcNow }
            }
        };

        // Out of scope status (Scheduled instead of Completed)
        var appScheduled = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job1, Candidate = candidate1, IsDeleted = false };
        var interviewScheduled = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = appScheduled.Id,
            Application = appScheduled,
            Status = InterviewStatus.Completed,
            IsDeleted = false,
            Participants = new List<InterviewParticipant>
            {
                new InterviewParticipant { FeedbackSubmittedAt = DateTime.UtcNow }
            }
        };

        // No feedback submitted
        var appNoFeedback = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job1, Candidate = candidate1, IsDeleted = false };
        var interviewNoFeedback = new Interview
        {
            Id = Guid.NewGuid(),
            ApplicationId = appNoFeedback.Id,
            Application = appNoFeedback,
            Status = InterviewStatus.Scheduled,
            IsDeleted = false,
            Participants = new List<InterviewParticipant>
            {
                new InterviewParticipant { FeedbackSubmittedAt = null } // NULL feedback
            }
        };

        var interviews = new List<Interview> { interview1, interviewOtherDept, interviewScheduled, interviewNoFeedback }
            .AsQueryable()
            .BuildMockDbSet();

        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        // Act
        var result = await _handler.Handle(new GetInterviewsForFeedbackQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().InterviewId.Should().Be(interview1.Id);
        result.Items.First().FeedbacksReceived.Should().Be(1);
    }
}
