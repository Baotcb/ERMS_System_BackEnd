using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Commands.SetEnterpriseStatus;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
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
                AdminNote = "Cho doi bo sung thong tin"
            },
            CancellationToken.None);

        result.Should().BeTrue();
        enterprise.Status.Should().Be(EnterpriseStatus.Suspended);
        addedHistory.Should().NotBeNull();
        addedHistory!.EntityType.Should().Be("Enterprise");
        addedHistory.Action.Should().Be("StatusChange");
        addedHistory.PreviousStatus.Should().Be(EnterpriseStatus.Active);
        addedHistory.NewStatus.Should().Be(EnterpriseStatus.Suspended);
        addedHistory.Note.Should().Be("Cho doi bo sung thong tin");
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
                NewStatus = EnterpriseStatus.Locked,
                AdminNote = "  Khoa do vi pham  "
            },
            CancellationToken.None);

        addedHistory.Should().NotBeNull();
        addedHistory!.Note.Should().Be("Khoa do vi pham");
    }
}
