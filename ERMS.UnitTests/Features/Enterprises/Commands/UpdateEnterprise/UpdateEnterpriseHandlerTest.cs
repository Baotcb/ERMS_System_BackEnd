using ERMS.Application.Features.Enterprises.Commands.UpdateEnterprise;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
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

namespace ERMS.UnitTests.Features.EnterpriseStatusConstants.Commands.UpdateEnterprise
{
    public class UpdateEnterpriseHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly UpdateEnterpriseHandler _handler;
        private readonly Mock<DbSet<ERMS.Domain.Entities.Enterprise.Enterprise>> _enterprisesMock;

        public UpdateEnterpriseHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            // Setup DbSet Mock
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise>();
            _enterprisesMock = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);

            _handler = new UpdateEnterpriseHandler(_contextMock.Object, _currentUserServiceMock.Object);
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
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new UpdateEnterpriseCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Không tìm thấy thông tin người dùng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotHR()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { "User" });
            var command = new UpdateEnterpriseCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ HR Manager mới có quyền cập nhật thông tin doanh nghiệp.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUpdateTooSoon()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });

            var enterpriseId = Guid.NewGuid();
            var enterprise = new ERMS.Domain.Entities.Enterprise.Enterprise
            {
                Id = enterpriseId,
                UpdatedAt = DateTime.UtcNow.AddMonths(-1), // Updated 1 month ago
                IsDeleted = false
            };

            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { enterprise };
            var mockSet = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(mockSet.Object);

            var command = new UpdateEnterpriseCommand { Id = enterpriseId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Chưa đủ thời gian để cập nhật*");
        }

        [Fact]
        public async Task Handle_ShouldUpdate_WhenAllowed()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });

            var enterpriseId = Guid.NewGuid();
            var enterprise = new ERMS.Domain.Entities.Enterprise.Enterprise
            {
                Id = enterpriseId,
                UpdatedAt = DateTime.UtcNow.AddMonths(-7), // Updated 7 months ago
                IsDeleted = false,
                EnterpriseName = "Old Name"
            };

            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { enterprise };
            var mockSet = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(mockSet.Object);

            var command = new UpdateEnterpriseCommand 
            { 
                Id = enterpriseId, 
                EnterpriseName = "New Name",
                Address = "New Address"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            enterprise.EnterpriseName.Should().Be("New Name");
            enterprise.Address.Should().Be("New Address");
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task Handle_ShouldThrowException_WhenNameExists()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.HRManager });

            var enterpriseId = Guid.NewGuid();
            var enterprise = new ERMS.Domain.Entities.Enterprise.Enterprise
            {
                Id = enterpriseId,
                UpdatedAt = DateTime.UtcNow.AddMonths(-7),
                IsDeleted = false,
                EnterpriseName = "Old Name"
            };
            
            var existingEnterprise = new ERMS.Domain.Entities.Enterprise.Enterprise
            {
                Id = Guid.NewGuid(),
                EnterpriseName = "Existing Name",
                IsDeleted = false
            };

            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise> { enterprise, existingEnterprise };
            var mockSet = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(mockSet.Object);

            var command = new UpdateEnterpriseCommand 
            { 
                Id = enterpriseId, 
                EnterpriseName = "Existing Name" 
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tên doanh nghiệp 'Existing Name' đã tồn tại trong hệ thống.");
        }
    }
}
