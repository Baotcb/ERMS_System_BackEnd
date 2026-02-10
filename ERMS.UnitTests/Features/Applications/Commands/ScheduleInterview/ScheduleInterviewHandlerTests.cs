using ERMS.Application.Features.Applications.Commands.ScheduleInterview;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Identity;
using ERMS.UnitTests.Helpers; 
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

using ApplicationEntity = ERMS.Domain.Entities.Application.Application;

namespace ERMS.UnitTests.Features.Applications.Commands.ScheduleInterview;

public class ScheduleInterviewHandlerTests
{
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<ScheduleInterviewHandler>> _loggerMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly ScheduleInterviewHandler _handler;
    private readonly ScheduleInterviewValidator _validator;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _departmentId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _jobPostingId = Guid.NewGuid();

    public ScheduleInterviewHandlerTests()
    {
        _contextMock = new Mock<IERMSDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<ScheduleInterviewHandler>>();
        _transactionMock = new Mock<IDbContextTransaction>();

        // Setup Transaction Mock - DatabaseFacade requires a DbContext instance for constructor
        var dbContextMock = new Mock<DbContext>();
        var databaseFacadeMock = new Mock<DatabaseFacade>(dbContextMock.Object);
        databaseFacadeMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _contextMock.Setup(c => c.Database).Returns(databaseFacadeMock.Object);

        _handler = new ScheduleInterviewHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);

        _validator = new ScheduleInterviewValidator();
    }

    #region Helper Methods

    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        // Setup Add methods to capture added entities
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
        mockSet.Setup(m => m.AddRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(data.AddRange);

        return mockSet;
    }

    private readonly int _departmentIdInt = 101;

    private void SetupAuthenticatedDepartmentHeadCorrect()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
        _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(_departmentIdInt);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(_enterpriseId);
    }
    
    private ApplicationEntity CreateApplication(JobPosting jobPosting)
    {
        return new ApplicationEntity
        {
            Id = _applicationId,
            JobPostingId = jobPosting.Id,
            JobPosting = jobPosting,
            Stage = ApplicationStage.Shortlisted,
            Status = "Active",
            IsDeleted = false
        };
    }

    private ScheduleInterviewCommand CreateValidCommand(List<Guid> interviewerIds)
    {
        return new ScheduleInterviewCommand
        {
            ApplicationId = _applicationId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            InterviewType = "Technical",
            Location = "Room 101",
            MeetingLink = "https://meet.google.com/abc-xyz",
            Note = "Prepare coding questions",
            InterviewerIds = interviewerIds
        };
    }

    #endregion

    #region Handler Tests - Happy Path

    [Fact]
    public async Task Handle_ShouldScheduleInterview_WhenRequestIsValid()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();

        var jobPosting = new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            DepartmentId = _departmentIdInt
        };

        var application = CreateApplication(jobPosting);
        var applicationsList = new List<ApplicationEntity> { application };
        var applicationsMock = CreateMockDbSet(applicationsList);
        _contextMock.Setup(c => c.Applications).Returns(applicationsMock.Object);

        var interviewerId = Guid.NewGuid();
        var interviewer = new Employee
        {
            Id = interviewerId,
            EnterpriseId = _enterpriseId,
            IsDeleted = false,
            User = new User { FullName = "Interviewer Name" }
        };
        var employeesList = new List<Employee> { interviewer };
        var employeesMock = CreateMockDbSet(employeesList);
        _contextMock.Setup(c => c.Employees).Returns(employeesMock.Object);

        var interviewsList = new List<Interview>();
        var interviewsMock = CreateMockDbSet(interviewsList);
        _contextMock.Setup(c => c.Interviews).Returns(interviewsMock.Object);

        var participantsList = new List<InterviewParticipant>();
        var participantsMock = CreateMockDbSet(participantsList);
        _contextMock.Setup(c => c.InterviewParticipants).Returns(participantsMock.Object);

        var command = CreateValidCommand(new List<Guid> { interviewerId });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // 1. Check Result
        result.Should().NotBeNull();
        result.InterviewId.Should().NotBeEmpty();
        result.ApplicationId.Should().Be(_applicationId);
        result.NewStage.Should().Be(ApplicationStage.InterviewScheduled);
        result.RoundNumber.Should().Be(1);
        result.Participants.Should().HaveCount(1);
        result.Participants.First().EmployeeId.Should().Be(interviewerId);

        // 2. Data Integrity - Application Stage
        application.Stage.Should().Be(ApplicationStage.InterviewScheduled);
        application.StageUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // 3. Data Integrity - Interview Record
        interviewsList.Should().HaveCount(1);
        var savedInterview = interviewsList.First();
        savedInterview.ApplicationId.Should().Be(_applicationId);
        savedInterview.RoundNumber.Should().Be(1);
        savedInterview.Status.Should().Be("Scheduled");
        savedInterview.ScheduledById.Should().Be(_userId);
        
        // 4. Data Integrity - Participants
        participantsList.Should().HaveCount(1);
        var savedParticipant = participantsList.First();
        savedParticipant.InterviewId.Should().Be(savedInterview.Id);
        savedParticipant.EmployeeId.Should().Be(interviewerId);
        savedParticipant.Role.Should().Be("Interviewer");

        // 5. Verify Transaction & Save
        _contextMock.Verify(c => c.Database.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Handler Tests - Security

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotDepartmentHead()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager }); // Wrong role
        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only Department Head can schedule interviews.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotAssociatedWithDepartment()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
        _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync((int?)null); // No department
        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not associated with any department.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserNotAssociatedWithEnterprise()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.DepartmentHead });
        _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(_departmentIdInt);
        _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null); // No enterprise
        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not associated with any enterprise.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenEnterpriseMismatch()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();

        var jobPosting = new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = Guid.NewGuid(), // Different Enterprise
            DepartmentId = _departmentIdInt
        };

        var application = CreateApplication(jobPosting);
        var applicationsList = new List<ApplicationEntity> { application };
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(applicationsList).Object);

        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You do not have permission to access this application.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenUserDepartmentMismatch()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();

        var jobPosting = new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            DepartmentId = _departmentIdInt + 1 // Different Department
        };

        var application = CreateApplication(jobPosting);
        var applicationsList = new List<ApplicationEntity> { application };
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(applicationsList).Object);

        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You can only schedule interviews for candidates in your department.");
    }

    #endregion

    #region Handler Tests - Business Logic

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationNotFound()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();
        var applicationsList = new List<ApplicationEntity>(); // Empty
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(applicationsList).Object);

        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"Application with ID {command.ApplicationId} not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenApplicationNotShortlisted()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();

        var jobPosting = new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            DepartmentId = _departmentIdInt
        };

        var application = CreateApplication(jobPosting);
        application.Stage = ApplicationStage.Applied; // Not Shortlisted

        var applicationsList = new List<ApplicationEntity> { application };
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(applicationsList).Object);

        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"Cannot schedule interview. Application stage is '{ApplicationStage.Applied}', expected '{ApplicationStage.Shortlisted}'.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenInterviewerNotFound()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();

        var jobPosting = new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            DepartmentId = _departmentIdInt
        };
        var application = CreateApplication(jobPosting);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }).Object);

        var missingInterviewerId = Guid.NewGuid();
        var employeesList = new List<Employee>(); // Empty employees
        _contextMock.Setup(c => c.Employees).Returns(CreateMockDbSet(employeesList).Object);

        var command = CreateValidCommand(new List<Guid> { missingInterviewerId });

        // Act & Assert
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"Some interviewers were not found: {missingInterviewerId}");
    }
    
    [Fact]
    public async Task Handle_ShouldThrowException_WhenInterviewerInDifferentEnterprise()
    {
        // Arrange
        SetupAuthenticatedDepartmentHeadCorrect();

        var jobPosting = new JobPosting
        {
            Id = _jobPostingId,
            EnterpriseId = _enterpriseId,
            DepartmentId = _departmentIdInt
        };
        var application = CreateApplication(jobPosting);
        _contextMock.Setup(c => c.Applications).Returns(CreateMockDbSet(new List<ApplicationEntity> { application }).Object);

        var interviewerId = Guid.NewGuid();
        var interviewer = new Employee
        {
            Id = interviewerId,
            EnterpriseId = Guid.NewGuid(), // Different Enterprise
            IsDeleted = false,
            User = new User { FullName = "External Person" }
        };

        var employeesList = new List<Employee> { interviewer };
        _contextMock.Setup(c => c.Employees).Returns(CreateMockDbSet(employeesList).Object);

        var command = CreateValidCommand(new List<Guid> { interviewerId });

        // Act & Assert
        // The handler filters by enterpriseId, so if not found in filtered list, it throws "not found"
        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage($"Some interviewers were not found: {interviewerId}");
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenApplicationIdEmpty()
    {
        var command = new ScheduleInterviewCommand { ApplicationId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId)
              .WithErrorMessage("ApplicationId is required.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenScheduledAtInPast()
    {
        var command = new ScheduleInterviewCommand { ScheduledAt = DateTime.UtcNow.AddMinutes(-10) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ScheduledAt)
              .WithErrorMessage("ScheduledAt must be in the future.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDurationInvalid()
    {
        var commandTooShort = new ScheduleInterviewCommand { Duration = 10 };
        var resultShort = _validator.TestValidate(commandTooShort);
        resultShort.ShouldHaveValidationErrorFor(x => x.Duration)
                   .WithErrorMessage("Duration must be between 15 and 480 minutes.");

        var commandTooLong = new ScheduleInterviewCommand { Duration = 500 };
        var resultLong = _validator.TestValidate(commandTooLong);
        resultLong.ShouldHaveValidationErrorFor(x => x.Duration)
                  .WithErrorMessage("Duration must be between 15 and 480 minutes.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenInterviewTypeEmptyOrTooLong()
    {
        var commandEmpty = new ScheduleInterviewCommand { InterviewType = "" };
        _validator.TestValidate(commandEmpty)
            .ShouldHaveValidationErrorFor(x => x.InterviewType)
            .WithErrorMessage("InterviewType is required.");

        var commandLong = new ScheduleInterviewCommand { InterviewType = new string('a', 51) };
        _validator.TestValidate(commandLong)
            .ShouldHaveValidationErrorFor(x => x.InterviewType)
            .WithErrorMessage("InterviewType must not exceed 50 characters.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenInterviewerIdsEmpty()
    {
        var command = new ScheduleInterviewCommand { InterviewerIds = new List<Guid>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InterviewerIds)
              .WithErrorMessage("At least one interviewer is required.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenInterviewerIdsContainEmptyGuid()
    {
        var command = new ScheduleInterviewCommand { InterviewerIds = new List<Guid> { Guid.NewGuid(), Guid.Empty } };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.InterviewerIds)
              .WithErrorMessage("InterviewerIds cannot contain empty GUIDs.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNoteTooLong()
    {
        var command = new ScheduleInterviewCommand { Note = new string('a', 2001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Note must not exceed 2000 characters.");
    }
    
    [Fact]
    public void Validate_ShouldHaveError_WhenLocationTooLong()
    {
        var command = new ScheduleInterviewCommand { Location = new string('a', 501) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Location)
              .WithErrorMessage("Location must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenMeetingLinkTooLong()
    {
        var command = new ScheduleInterviewCommand { MeetingLink = new string('a', 501) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MeetingLink)
              .WithErrorMessage("MeetingLink must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var command = new ScheduleInterviewCommand
        {
            ApplicationId = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Duration = 60,
            InterviewType = "Technical",
            InterviewerIds = new List<Guid> { Guid.NewGuid() },
            Location = "Office",
            MeetingLink = "https://zoom.us",
            Note = "Test Note"
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
