using ERMS.Application.Features.Auth.Commands.ChangePassword;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.ChangePassword
{
    public class ChangePasswordHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly ChangePasswordHandler _handler;

        public ChangePasswordHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new ChangePasswordHandler(_userManagerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new ChangePasswordCommand { CurrentPassword = "OldPassword", NewPassword = "NewPassword" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Không tìm thấy thông tin người dùng.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User)null!);
            
            var command = new ChangePasswordCommand { CurrentPassword = "OldPassword", NewPassword = "NewPassword" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy người dùng");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenChangePasswordFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            var command = new ChangePasswordCommand { CurrentPassword = "OldPassword", NewPassword = "NewPassword" };
            var identityError = new IdentityError { Description = "Wrong password" };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Thay đổi mật khẩu thất bại: {identityError.Description}");
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessMessage_WhenChangePasswordSucceeds()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            var command = new ChangePasswordCommand { CurrentPassword = "OldPassword", NewPassword = "NewPassword" };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be("Thay đổi mật khẩu thành công");
        }
    }
}
