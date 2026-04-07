using System.Reflection;
using ERMS.API.Controllers;
using ERMS.Application.Features.Admin.Commands.SetEnterpriseLockState;
using ERMS.Application.Features.Admin.Commands.SetEnterpriseStatus;
using ERMS.Application.Features.Admin.Queries.GetAdminDashboard;
using ERMS.Application.Features.Admin.Queries.GetAiServiceOverview;
using ERMS.Application.Features.Admin.Queries.GetEnterpriseAdminDetail;
using ERMS.Application.Features.Admin.Queries.GetEnterpriseList;
using ERMS.Application.Features.Admin.Queries.GetGlobalPaymentHistory;
using ERMS.Application.Features.Admin.Queries.GetPlatformStats;
using ERMS.Application.Features.Admin.Queries.GetSystemIntegrations;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ERMS.API.UnitTests;

[TestFixture]
public class AdminControllerTests
{
    private Mock<ISender> _senderMock = null!;
    private AdminController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _senderMock = new Mock<ISender>();
        _controller = new AdminController(_senderMock.Object);
    }

    [Test]
    public async Task GetDashboard_ShouldSendGetAdminDashboardQuery_AndReturnOk()
    {
        var response = new GetAdminDashboardResponse { TotalEnterprises = 5 };
        _senderMock
            .Setup(sender => sender.Send(It.IsAny<GetAdminDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetDashboard();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(
            sender => sender.Send(It.IsAny<GetAdminDashboardQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetEnterprises_ShouldForwardProvidedQuery_AndReturnOk()
    {
        var query = new GetEnterpriseListQuery { Search = "tech", PageNumber = 2, PageSize = 5 };
        var response = new GetEnterpriseListResponse { TotalCount = 1 };
        _senderMock
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetEnterprises(query);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetEnterpriseDetailById_ShouldWrapRouteIdIntoQuery_AndReturnOk()
    {
        var enterpriseId = Guid.NewGuid();
        var response = new GetEnterpriseAdminDetailResponse { EnterpriseId = enterpriseId };
        _senderMock
            .Setup(sender => sender.Send(It.IsAny<GetEnterpriseAdminDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetEnterpriseDetailById(enterpriseId);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(
            sender => sender.Send(
                It.Is<GetEnterpriseAdminDetailQuery>(query => query.EnterpriseId == enterpriseId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SetEnterpriseStatus_ShouldOverwriteRouteEnterpriseId_AndReturnSuccessPayload()
    {
        var routeEnterpriseId = Guid.NewGuid();
        var command = new SetEnterpriseStatusCommand
        {
            EnterpriseId = Guid.NewGuid(),
            NewStatus = "Locked",
            AdminNote = "test note"
        };

        _senderMock
            .Setup(sender => sender.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SetEnterpriseStatus(routeEnterpriseId, command);

        command.EnterpriseId.Should().Be(routeEnterpriseId);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        ReadAnonymousProperty<string>(okResult.Value!, "message")
            .Should().Be("Cập nhật trạng thái doanh nghiệp thành công.");
        ReadAnonymousProperty<bool>(okResult.Value!, "result").Should().BeTrue();
        _senderMock.Verify(sender => sender.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetEnterpriseLockState_ShouldReturnLockMessage_WhenIsLockedIsTrue()
    {
        var command = new SetEnterpriseLockStateCommand
        {
            EnterpriseId = Guid.NewGuid(),
            IsLocked = true
        };

        _senderMock
            .Setup(sender => sender.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SetEnterpriseLockState(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        ReadAnonymousProperty<string>(okResult.Value!, "message")
            .Should().Be("Khóa doanh nghiệp thành công.");
        ReadAnonymousProperty<bool>(okResult.Value!, "result").Should().BeTrue();
        _senderMock.Verify(sender => sender.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetEnterpriseLockState_ShouldReturnUnlockMessage_WhenIsLockedIsFalse()
    {
        var command = new SetEnterpriseLockStateCommand
        {
            EnterpriseId = Guid.NewGuid(),
            IsLocked = false
        };

        _senderMock
            .Setup(sender => sender.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.SetEnterpriseLockState(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        ReadAnonymousProperty<string>(okResult.Value!, "message")
            .Should().Be("Mở khóa doanh nghiệp thành công.");
        ReadAnonymousProperty<bool>(okResult.Value!, "result").Should().BeTrue();
        _senderMock.Verify(sender => sender.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetPaymentHistory_ShouldForwardProvidedQuery_AndReturnOk()
    {
        var query = new GetGlobalPaymentHistoryQuery { EnterpriseSearch = "tech", PageNumber = 1, PageSize = 10 };
        var response = new GetGlobalPaymentHistoryResponse { TotalCount = 1 };
        _senderMock
            .Setup(sender => sender.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetPaymentHistory(query);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(sender => sender.Send(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetPlatformStats_ShouldSendGetPlatformStatsQuery_AndReturnOk()
    {
        var response = new GetPlatformStatsResponse { TotalEnterprises = 5 };
        _senderMock
            .Setup(sender => sender.Send(It.IsAny<GetPlatformStatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetPlatformStats();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(
            sender => sender.Send(It.IsAny<GetPlatformStatsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAiServices_ShouldSendGetAiServiceOverviewQuery_AndReturnOk()
    {
        var response = new GetAiServiceOverviewResponse { ProviderName = "Gemini" };
        _senderMock
            .Setup(sender => sender.Send(It.IsAny<GetAiServiceOverviewQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetAiServices();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(
            sender => sender.Send(It.IsAny<GetAiServiceOverviewQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetSystemIntegrations_ShouldSendGetSystemIntegrationsQuery_AndReturnOk()
    {
        var response = new GetSystemIntegrationsResponse
        {
            new() { Name = "Gemini", Category = "AI", Status = "Configured", EnvironmentScope = "System" }
        };

        _senderMock
            .Setup(sender => sender.Send(It.IsAny<GetSystemIntegrationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.GetSystemIntegrations();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(response);
        _senderMock.Verify(
            sender => sender.Send(It.IsAny<GetSystemIntegrationsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static T ReadAnonymousProperty<T>(object instance, string propertyName)
    {
        return (T)instance.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(instance)!;
    }
}
