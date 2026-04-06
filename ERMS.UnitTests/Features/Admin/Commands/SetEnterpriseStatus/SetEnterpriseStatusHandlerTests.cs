using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Commands.SetEnterpriseStatus;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.System;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Commands.SetEnterpriseStatus;

public class SetEnterpriseStatusHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly SetEnterpriseStatusHandler _handler;

    public SetEnterpriseStatusHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new SetEnterpriseStatusHandler(_mockContext.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenCurrentUserIsNotAdmin()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.HRManager });

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = Guid.NewGuid(),
                NewStatus = EnterpriseStatus.Suspended
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldUpdateEnterpriseStatus_AndWriteApprovalHistory()
    {
        var userId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Active,
            IsDeleted = false
        };

        ApprovalHistory? addedHistory = null;
        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();
        approvalHistories.Setup(x => x.Add(It.IsAny<ApprovalHistory>()))
            .Callback<ApprovalHistory>(history => addedHistory = history);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Suspended,
                ReasonCategory = "Compliance",
                AdminNote = "Chờ đợi bổ sung thông tin"
            },
            CancellationToken.None);

        result.Should().BeTrue();
        enterprise.Status.Should().Be(EnterpriseStatus.Suspended);
        addedHistory.Should().NotBeNull();
        addedHistory!.EntityType.Should().Be("Enterprise");
        addedHistory.Action.Should().Be("StatusChange");
        addedHistory.PreviousStatus.Should().Be(EnterpriseStatus.Active);
        addedHistory.NewStatus.Should().Be(EnterpriseStatus.Suspended);
        addedHistory.Note.Should().Be("Compliance: Chờ đợi bổ sung thông tin");
        addedHistory.PerformedById.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenCurrentUserIsMissing()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = Guid.NewGuid(),
                NewStatus = EnterpriseStatus.Locked
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenStatusIsInvalid()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = Guid.NewGuid(),
                NewStatus = "Unknown"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenEnterpriseIdIsEmpty()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = Guid.Empty,
                NewStatus = EnterpriseStatus.Suspended
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseDoesNotExist()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = Guid.NewGuid(),
                NewStatus = EnterpriseStatus.Inactive
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseIsSoftDeleted()
    {
        var enterpriseId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>
        {
            new()
            {
                Id = enterpriseId,
                Status = EnterpriseStatus.Active,
                IsDeleted = true
            }
        }.AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterpriseId,
                NewStatus = EnterpriseStatus.Inactive
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WithoutSaving_WhenStatusIsUnchanged()
    {
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Active,
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);

        var result = await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Active
            },
            CancellationToken.None);

        result.Should().BeTrue();
        approvalHistories.Verify(x => x.Add(It.IsAny<ApprovalHistory>()), Times.Never);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenRolesAreMissing()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns((IEnumerable<string>?)null);

        var act = async () => await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = Guid.NewGuid(),
                NewStatus = EnterpriseStatus.Suspended
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldTrimAdminNoteBeforeWritingApprovalHistory()
    {
        var userId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Active,
            IsDeleted = false
        };

        ApprovalHistory? addedHistory = null;
        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();
        approvalHistories.Setup(x => x.Add(It.IsAny<ApprovalHistory>()))
            .Callback<ApprovalHistory>(history => addedHistory = history);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Suspended,
                AdminNote = "  Khoa do vi pham  "
            },
            CancellationToken.None);

        addedHistory.Should().NotBeNull();
        addedHistory!.Note.Should().Be("Khoa do vi pham");
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotifications_WhenSendNotificationIsFalse()
    {
        var userId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Active,
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();
        var notifications = new List<Notification>().AsQueryable().BuildMockDbSet();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);
        _mockContext.Setup(x => x.Notifications).Returns(notifications.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Suspended,
                SendNotification = false
            },
            CancellationToken.None);

        notifications.Verify(x => x.AddRange(It.IsAny<IEnumerable<Notification>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateNotificationsViaAddRange_WhenSendNotificationIsTrue()
    {
        var userId = Guid.NewGuid();
        var empUserId1 = Guid.NewGuid();
        var empUserId2 = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Test Corp",
            Status = EnterpriseStatus.Active,
            CreatedById = userId,
            IsDeleted = false
        };

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterprise.Id, UserId = empUserId1, EmployeeCode = "E1", IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterprise.Id, UserId = empUserId2, EmployeeCode = "E2", IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterprise.Id, UserId = userId, EmployeeCode = "E3", IsDeleted = false }
        };

        IEnumerable<Notification>? addedNotifications = null;
        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();
        var notificationsMock = new List<Notification>().AsQueryable().BuildMockDbSet();
        notificationsMock.Setup(x => x.AddRange(It.IsAny<IEnumerable<Notification>>()))
            .Callback<IEnumerable<Notification>>(n => addedNotifications = n.ToList());

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);
        _mockContext.Setup(x => x.Notifications).Returns(notificationsMock.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Suspended,
                SendNotification = true,
                ReasonCategory = "Compliance"
            },
            CancellationToken.None);

        addedNotifications.Should().NotBeNull();
        var list = addedNotifications!.ToList();
        list.Should().HaveCount(3);
        list.Select(n => n.UserId).Should().BeEquivalentTo(new[] { empUserId1, empUserId2, userId });
        list.Should().OnlyContain(n => n.NotificationType == "EnterpriseStatusChange");
        list.Should().OnlyContain(n => n.EntityId == enterprise.Id);
    }

    [Fact]
    public async Task Handle_ShouldIncludeCreatedById_WhenNotAlreadyInEmployeeList()
    {
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var empUserId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Owner Corp",
            Status = EnterpriseStatus.Active,
            CreatedById = ownerId,
            IsDeleted = false
        };

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterprise.Id, UserId = empUserId, EmployeeCode = "E1", IsDeleted = false }
        };

        IEnumerable<Notification>? addedNotifications = null;
        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();
        var notificationsMock = new List<Notification>().AsQueryable().BuildMockDbSet();
        notificationsMock.Setup(x => x.AddRange(It.IsAny<IEnumerable<Notification>>()))
            .Callback<IEnumerable<Notification>>(n => addedNotifications = n.ToList());

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);
        _mockContext.Setup(x => x.Notifications).Returns(notificationsMock.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Locked,
                SendNotification = true
            },
            CancellationToken.None);

        addedNotifications.Should().NotBeNull();
        var list = addedNotifications!.ToList();
        list.Should().HaveCount(2);
        list.Select(n => n.UserId).Should().Contain(ownerId);
        list.Select(n => n.UserId).Should().Contain(empUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotAddNotifications_WhenSendNotificationTrueButNoRecipients()
    {
        var userId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Empty Corp",
            Status = EnterpriseStatus.Active,
            CreatedById = null,
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>().AsQueryable().BuildMockDbSet();
        var notificationsMock = new List<Notification>().AsQueryable().BuildMockDbSet();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.Object);
        _mockContext.Setup(x => x.Notifications).Returns(notificationsMock.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _handler.Handle(
            new SetEnterpriseStatusCommand
            {
                EnterpriseId = enterprise.Id,
                NewStatus = EnterpriseStatus.Suspended,
                SendNotification = true
            },
            CancellationToken.None);

        notificationsMock.Verify(x => x.AddRange(It.IsAny<IEnumerable<Notification>>()), Times.Never);
    }

}
