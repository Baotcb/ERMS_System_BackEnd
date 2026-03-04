using ERMS.Application.Features.Enterprises.Commands.LockEnterprise;
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

namespace ERMS.UnitTests.Features.EnterpriseStatusConstants.Commands.LockEnterprise
{
    public class LockEnterpriseHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly LockEnterpriseHandler _handler;
        private readonly Mock<DbSet<ERMS.Domain.Entities.Enterprise.Enterprise>> _enterprisesMock;

        public LockEnterpriseHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            // Setup DbSet Mock
            var enterprises = new List<ERMS.Domain.Entities.Enterprise.Enterprise>();
            _enterprisesMock = CreateMockDbSet(enterprises);
            _contextMock.Setup(x => x.Enterprises).Returns(_enterprisesMock.Object);

            _handler = new LockEnterpriseHandler(_contextMock.Object, _currentUserServiceMock.Object);
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            // Setup Async support for FindAsync which might use FindAsync internally or we mock FindAsync on DbSet directly?
            // Actually Handler uses _context.Enterprises.FindAsync(request.EnterpriseId)
            // Moq's FindAsync on DbSet is tricky. Usually sticking to FirstOrDefaultAsync with predicate is easier for checking.
            // Converting Handler to use FirstOrDefaultAsync or mocking FindAsync.
            // Let's mock FindAsync specifically on the DbSet mock.
            
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
            var command = new LockEnterpriseCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Không tìm thấy thông tin người dùng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAdmin()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { "User" });
            var command = new LockEnterpriseCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Admin mới có quyền chỉnh sửa trạng thái doanh nghiệp .");
        }

        [Fact]
        public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Admin });
            
            var command = new LockEnterpriseCommand { EnterpriseId = Guid.NewGuid() };
            _enterprisesMock.Setup(x => x.FindAsync(command.EnterpriseId))
                .ReturnsAsync((ERMS.Domain.Entities.Enterprise.Enterprise)null!);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Không tìm thấy doanh nghiệp với mã {command.EnterpriseId}.");
        }

        [Fact]
        public async Task Handle_ShouldLockEnterprise_WhenIsLockedIsTrue()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Admin });

            var enterpriseId = Guid.NewGuid();
            var enterprise = new ERMS.Domain.Entities.Enterprise.Enterprise 
            { 
                Id = enterpriseId, 
                Status = ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Active 
            };

            var command = new LockEnterpriseCommand { EnterpriseId = enterpriseId, IsLocked = true };
            _enterprisesMock.Setup(x => x.FindAsync(command.EnterpriseId))
                .ReturnsAsync(enterprise);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            enterprise.Status.Should().Be(ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Locked);
            _enterprisesMock.Verify(x => x.Update(enterprise), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUnlockEnterprise_WhenIsLockedIsFalse()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new List<string> { AppRoles.Admin });

            var enterpriseId = Guid.NewGuid();
            var enterprise = new ERMS.Domain.Entities.Enterprise.Enterprise 
            { 
                Id = enterpriseId, 
                Status = ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Locked 
            };

            var command = new LockEnterpriseCommand { EnterpriseId = enterpriseId, IsLocked = false };
            _enterprisesMock.Setup(x => x.FindAsync(command.EnterpriseId))
                .ReturnsAsync(enterprise);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            enterprise.Status.Should().Be(ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Active);
            _enterprisesMock.Verify(x => x.Update(enterprise), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
