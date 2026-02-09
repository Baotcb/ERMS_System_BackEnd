using ERMS.Application.Features.Users.Commands.LockUser;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Users.Commands.LockUser
{
    public class LockUserHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly LockUserHandler _handler;

        public LockUserHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _handler = new LockUserHandler(_userManagerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            var command = new LockUserCommand { UserId = Guid.NewGuid(), IsLocked = true };
            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldLockUser_WhenIsLockedIsTrue()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid() };
            var command = new LockUserCommand { UserId = user.Id, IsLocked = true };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
            _userManagerMock.Verify(x => x.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUnlockUser_WhenIsLockedIsFalse()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid() };
            var command = new LockUserCommand { UserId = user.Id, IsLocked = false };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _userManagerMock.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
            // Ensure we don't accidentally lock or set lockout enabled when unlocking (logic check)
            _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Never);
        }
    }
}
