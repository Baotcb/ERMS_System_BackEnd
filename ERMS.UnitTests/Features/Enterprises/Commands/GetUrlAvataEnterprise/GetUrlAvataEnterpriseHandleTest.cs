using ERMS.Application.Features.Enterprises.Commands.GetUrlAvataEnterprise;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.EnterpriseStatusConstants.Commands.GetUrlAvataEnterprise
{
    public class GetUrlAvataEnterpriseHandleTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetUrlAvataEnterpriseHandle _handler;
        private readonly Mock<DbSet<ERMS.Domain.Entities.Enterprise.Enterprise>> _enterprisesMock;

        public GetUrlAvataEnterpriseHandleTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            // Setup DbSet Mock
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise>();
            _enterprisesMock = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);

            _handler = new GetUrlAvataEnterpriseHandle(_contextMock.Object, _currentUserServiceMock.Object);
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
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

            return mockSet;
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new GetUrlAvataEnterpriseCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        [Fact]
        public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseNotFound()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            // Mock Empty DbSet
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise>();
            var mockSet = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(mockSet.Object);

            var command = new GetUrlAvataEnterpriseCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Không tìm thấy doanh nghiệp với ID {enterpriseId}.");
        }

        [Fact]
        public async Task Handle_ShouldReturnResponse_WhenEnterpriseFound()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var enterprise = new ERMS.Domain.Entities.Enterprise.Enterprise
            {
                Id = enterpriseId,
                EnterpriseName = "Test Enterprise",
                LogoUrl = "http://example.com/logo.png",
                IsDeleted = false
            };

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { enterprise };
            var mockSet = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(mockSet.Object);

            var command = new GetUrlAvataEnterpriseCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.EnterpriseName.Should().Be(enterprise.EnterpriseName);
            result.LogoUrl.Should().Be(enterprise.LogoUrl);
        }
    }
}
