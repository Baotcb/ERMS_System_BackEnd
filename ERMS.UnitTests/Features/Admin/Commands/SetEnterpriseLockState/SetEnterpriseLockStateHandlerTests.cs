using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Commands.SetEnterpriseLockState;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Commands.SetEnterpriseLockState;

public class SetEnterpriseLockStateHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly SetEnterpriseLockStateHandler _handler;

    public SetEnterpriseLockStateHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new SetEnterpriseLockStateHandler(_mockContext.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdIsMissing()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var act = async () => await _handler.Handle(
            new SetEnterpriseLockStateCommand { EnterpriseId = Guid.NewGuid(), IsLocked = true },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenCurrentUserIsNotAdmin()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.HRManager });

        var act = async () => await _handler.Handle(
            new SetEnterpriseLockStateCommand { EnterpriseId = Guid.NewGuid(), IsLocked = true },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseIsMissingOrSoftDeleted()
    {
        var deletedEnterpriseId = Guid.NewGuid();
        var enterprises = new List<Enterprise>
        {
            new() { Id = deletedEnterpriseId, Status = EnterpriseStatus.Active, IsDeleted = true }
        };

        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(enterprises.AsQueryable().BuildMockDbSet().Object);

        var missingAct = async () => await _handler.Handle(
            new SetEnterpriseLockStateCommand { EnterpriseId = Guid.NewGuid(), IsLocked = true },
            CancellationToken.None);

        var deletedAct = async () => await _handler.Handle(
            new SetEnterpriseLockStateCommand { EnterpriseId = deletedEnterpriseId, IsLocked = true },
            CancellationToken.None);

        await missingAct.Should().ThrowAsync<KeyNotFoundException>();
        await deletedAct.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldSetLockedStatus_WhenIsLockedIsTrue()
    {
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Active,
            IsDeleted = false
        };

        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(
            new SetEnterpriseLockStateCommand { EnterpriseId = enterprise.Id, IsLocked = true },
            CancellationToken.None);

        result.Should().BeTrue();
        enterprise.Status.Should().Be(EnterpriseStatus.Locked);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateApprovalHistory_WhenLockingEnterprise()
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
            new SetEnterpriseLockStateCommand { EnterpriseId = enterprise.Id, IsLocked = true },
            CancellationToken.None);

        result.Should().BeTrue();
        enterprise.Status.Should().Be(EnterpriseStatus.Locked);
        addedHistory.Should().NotBeNull();
        addedHistory!.EntityType.Should().Be("Enterprise");
        addedHistory.EntityId.Should().Be(enterprise.Id);
        addedHistory.Action.Should().Be("Lock");
        addedHistory.PreviousStatus.Should().Be(EnterpriseStatus.Active);
        addedHistory.NewStatus.Should().Be(EnterpriseStatus.Locked);
        addedHistory.PerformedById.Should().Be(userId);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetActiveStatus_WhenIsLockedIsFalse()
    {
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Locked,
            IsDeleted = false
        };

        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.Roles).Returns(new[] { AppRoles.Admin });
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(
            new SetEnterpriseLockStateCommand { EnterpriseId = enterprise.Id, IsLocked = false },
            CancellationToken.None);

        result.Should().BeTrue();
        enterprise.Status.Should().Be(EnterpriseStatus.Active);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateApprovalHistory_WhenUnlockingEnterprise()
    {
        var userId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            Status = EnterpriseStatus.Locked,
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
            new SetEnterpriseLockStateCommand { EnterpriseId = enterprise.Id, IsLocked = false },
            CancellationToken.None);

        result.Should().BeTrue();
        enterprise.Status.Should().Be(EnterpriseStatus.Active);
        addedHistory.Should().NotBeNull();
        addedHistory!.EntityType.Should().Be("Enterprise");
        addedHistory.EntityId.Should().Be(enterprise.Id);
        addedHistory.Action.Should().Be("Unlock");
        addedHistory.PreviousStatus.Should().Be(EnterpriseStatus.Locked);
        addedHistory.NewStatus.Should().Be(EnterpriseStatus.Active);
        addedHistory.PerformedById.Should().Be(userId);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
