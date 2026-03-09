using ERMS.Application.Features.Enterprises.Queries.GetEnterpriseDetails;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Enterprises.Queries.GetEnterpriseDetails;

public class GetEnterpriseDetailsHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly Mock<ILogger<GetEnterpriseDetailsHandler>> _mockLogger;
    private readonly GetEnterpriseDetailsHandler _handler;

    public GetEnterpriseDetailsHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _mockLogger = new Mock<ILogger<GetEnterpriseDetailsHandler>>();
        _handler = new GetEnterpriseDetailsHandler(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnEnterpriseDetails_WhenFoundAndActive()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Tech Corp",
            EnterpriseCode = "TC001",
            Address = "123 Tech Lane",
            Phone = "555-1234",
            Email = "contact@techcorp.xyz",
            Website = "https://techcorp.xyz",
            LogoUrl = "https://cdn.example.com/logo.png",
            Status = "Active",
            IsDeleted = false,
            TaxCode = "SENSITIVE-TAX-123", // Should be filtered out
            SubscriptionStatus = "Active"  // Should be filtered out
        };

        var enterprises = new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Enterprises).Returns(enterprises.Object);

        var query = new GetEnterpriseDetailsQuery { Id = enterpriseId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(enterpriseId);
        result.EnterpriseName.Should().Be("Tech Corp");
        result.EnterpriseCode.Should().Be("TC001");
        result.Address.Should().Be("123 Tech Lane");
        result.Phone.Should().Be("555-1234");
        result.Email.Should().Be("contact@techcorp.xyz");
        result.Website.Should().Be("https://techcorp.xyz");
        result.LogoUrl.Should().Be("https://cdn.example.com/logo.png");
        
        // Ensure sensitive fields are not present in the response type at all
        var responseType = result.GetType();
        responseType.GetProperty("TaxCode").Should().BeNull();
        responseType.GetProperty("SubscriptionStatus").Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenEnterpriseNotFound()
    {
        // Arrange
        var enterprises = new List<Enterprise>().AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Enterprises).Returns(enterprises.Object);

        var query = new GetEnterpriseDetailsQuery { Id = Guid.NewGuid() };

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"Không tìm thấy doanh nghiệp với ID {query.Id} hoặc hiện không khả dụng.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenEnterpriseIsDeleted()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Tech Corp",
            Status = "Active",
            IsDeleted = true // Deleted!
        };

        var enterprises = new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Enterprises).Returns(enterprises.Object);

        var query = new GetEnterpriseDetailsQuery { Id = enterpriseId };

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"Không tìm thấy doanh nghiệp với ID {query.Id} hoặc hiện không khả dụng.");
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenEnterpriseIsNotActive()
    {
        // Arrange
        var enterpriseId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Tech Corp",
            Status = "Suspended", // Inactive!
            IsDeleted = false
        };

        var enterprises = new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Enterprises).Returns(enterprises.Object);

        var query = new GetEnterpriseDetailsQuery { Id = enterpriseId };

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"Không tìm thấy doanh nghiệp với ID {query.Id} hoặc hiện không khả dụng.");
    }
}
