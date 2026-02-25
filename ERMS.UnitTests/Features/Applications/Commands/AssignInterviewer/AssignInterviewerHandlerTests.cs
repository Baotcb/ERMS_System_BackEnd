using ERMS.Application.Features.Applications.Commands.AssignInterviewer;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using ApplicationEntity = ERMS.Domain.Entities.Application.Application;
using Xunit;

namespace ERMS.UnitTests.Features.Applications.Commands.AssignInterviewer;

public class AssignInterviewerHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<AssignInterviewerHandler>> _mockLogger;
    private readonly AssignInterviewerHandler _handler;

    public AssignInterviewerHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<AssignInterviewerHandler>>();
        _handler = new AssignInterviewerHandler(_mockContext.Object, _mockCurrentUserService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreatePendingInterview_WhenUserIsDepartmentHead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var departmentId = 1;
        var enterpriseId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var interviewerId = Guid.NewGuid();

        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.DepartmentHead]);
        _mockCurrentUserService.Setup(s => s.GetDepartmentIdAsync()).ReturnsAsync(departmentId);
        _mockCurrentUserService.Setup(s => s.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

        var jobPosting = new JobPosting { Id = Guid.NewGuid(), DepartmentId = departmentId, EnterpriseId = enterpriseId };
        var application = new ApplicationEntity { Id = applicationId, JobPosting = jobPosting, Stage = ApplicationStage.Shortlisted };
        var interviewer = new Employee { Id = interviewerId, EnterpriseId = enterpriseId, User = new User { FullName = "Interviewer" } };

        var applications = new List<ApplicationEntity> { application }.AsQueryable().BuildMockDbSet();
        var employees = new List<Employee> { interviewer }.AsQueryable().BuildMockDbSet();
        var interviews = new List<Interview>().AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Applications).Returns(applications.Object);
        _mockContext.Setup(c => c.Employees).Returns(employees.Object);
        _mockContext.Setup(c => c.Interviews).Returns(interviews.Object);
        _mockContext.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IDbContextTransaction>().Object);

        var command = new AssignInterviewerCommand
        {
            ApplicationId = applicationId,
            InterviewType = "Technical",
            InterviewerIds = [interviewerId]
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(InterviewStatus.PendingSchedule);
        
        // Use verify to check if Add was called on DbSet
        _mockContext.Verify(c => c.Interviews.Add(It.Is<Interview>(i => 
            i.Status == InterviewStatus.PendingSchedule &&
            i.Participants.Count == 1 &&
            i.Participants.First().EmployeeId == interviewerId
        )), Times.Once);

        // Verify Status was NOT updated
        application.Stage.Should().Be(ApplicationStage.Shortlisted);
        
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserIsNotDepartmentHead()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(s => s.Roles).Returns([AppRoles.Employee]); // Not Dept Head

        var command = new AssignInterviewerCommand { ApplicationId = Guid.NewGuid() };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only Department Head can assign interviewers.");
    }
}
