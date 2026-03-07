using ERMS.Application.Features.Interviews.Queries.GetInterviewFeedbackById;
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

namespace ERMS.UnitTests.Features.Interviews.Queries.GetInterviewFeedbackById;

public class GetInterviewFeedbackByIdHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetInterviewFeedbackByIdHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly int _departmentId = 1;
    private readonly Guid _interviewId = Guid.NewGuid();

    public GetInterviewFeedbackByIdHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new GetInterviewFeedbackByIdHandler(_mockContext.Object, _mockCurrentUserService.Object);
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
        var act = async () => await _handler.Handle(new GetInterviewFeedbackByIdQuery { InterviewId = _interviewId }, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Người dùng chưa được xác thực.");
    }

    [Fact]
    public async Task Handle_NotDepartmentHead_ThrowsUnauthorizedAccessException()
    {
        _mockCurrentUserService.Setup(s => s.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns(new List<string> { AppRoles.Employee });
        var act = async () => await _handler.Handle(new GetInterviewFeedbackByIdQuery { InterviewId = _interviewId }, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Chỉ Trưởng phòng mới có quyền xem chi tiết đánh giá phỏng vấn.");
    }

    [Fact]
    public async Task Handle_InterviewNotFound_ThrowsException()
    {
        SetupAuthenticatedDepartmentHead();
        var interviews = new List<Interview>().AsQueryable().BuildMockDbSet();
        _mockContext.Setup(x => x.Interviews).Returns(interviews.Object);

        var act = async () => await _handler.Handle(new GetInterviewFeedbackByIdQuery { InterviewId = _interviewId }, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>().WithMessage($"Không tìm thấy buổi phỏng vấn với ID {_interviewId}.");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsDetailedFeedback()
    {
        // Arrange
        SetupAuthenticatedDepartmentHead();

        var user = new User { FullName = "John Doe", Email = "j@d.com" };
        var employeeUser = new User { FullName = "Interviewer One", Email = "int@d.com" };
        var employee = new Employee { User = employeeUser };

        var candidate = new Candidate { User = user };
        var job = new JobPosting { EnterpriseId = _enterpriseId, DepartmentId = _departmentId, JobTitle = "Role" };
        var app = new ApplicationEntity { Id = Guid.NewGuid(), Candidate = candidate, JobPosting = job, IsDeleted = false };

        var interview = new Interview
        {
            Id = _interviewId,
            ApplicationId = app.Id,
            Application = app,
            Status = InterviewStatus.Scheduled,
            RoundNumber = 1,
            InterviewType = "Tech",
            IsDeleted = false,
            Decision = "Passed",
            Participants = new List<InterviewParticipant>
            {
                new InterviewParticipant 
                { 
                    Id = Guid.NewGuid(),
                    Employee = employee, 
                    FeedbackSubmittedAt = DateTime.UtcNow, 
                    Role = "Interviewer",
                    Rating = 5,
                    Feedback = "Great",
                    Recommendation = "Hire"
                }
            }
        };

        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        // Act
        var result = await _handler.Handle(new GetInterviewFeedbackByIdQuery { InterviewId = _interviewId }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.InterviewId.Should().Be(_interviewId);
        result.CandidateName.Should().Be("John Doe");
        result.ParticipantsFeedback.Should().HaveCount(1);
        result.ParticipantsFeedback.First().Rating.Should().Be(5);
        result.DepartmentHeadDecision.Should().Be("Passed");
    }

    [Fact]
    public async Task Handle_CrossDepartmentAccess_ThrowsUnauthorized()
    {
        // Arrange
        SetupAuthenticatedDepartmentHead(); // Has DepartmentId = 1

        var job = new JobPosting { EnterpriseId = _enterpriseId, DepartmentId = 999, JobTitle = "Role" };
        var app = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job, IsDeleted = false };
        var interview = new Interview { Id = _interviewId, Application = app, IsDeleted = false };

        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        // Act
        var act = async () => await _handler.Handle(new GetInterviewFeedbackByIdQuery { InterviewId = _interviewId }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Bạn không có quyền truy cập buổi phỏng vấn của phòng ban khác.");
    }

    [Fact]
    public async Task Handle_NotCompletedStatus_ThrowsException()
    {
        SetupAuthenticatedDepartmentHead();

        var job = new JobPosting { EnterpriseId = _enterpriseId, DepartmentId = _departmentId, JobTitle = "Role" };
        var app = new ApplicationEntity { Id = Guid.NewGuid(), JobPosting = job, IsDeleted = false };
        var interview = new Interview { Id = _interviewId, Application = app, Status = InterviewStatus.Completed, IsDeleted = false };

        var interviews = new List<Interview> { interview }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);

        var act = async () => await _handler.Handle(new GetInterviewFeedbackByIdQuery { InterviewId = _interviewId }, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>().WithMessage("Xem đánh giá chỉ khả dụng cho các buổi phỏng vấn đã lên lịch đang chờ quyết định.");
    }
}
