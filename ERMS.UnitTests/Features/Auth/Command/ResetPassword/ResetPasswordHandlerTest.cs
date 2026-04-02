using ERMS.Application.Features.Auth.Commands.ResetPassword;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.ResetPassword
{
    public class ResetPasswordHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly ResetPasswordHandler _handler;

        public ResetPasswordHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _handler = new ResetPasswordHandler(_userManagerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var command = new ResetPasswordCommand
            {
                Email = "notfound@example.com",
                Token = "valid-token",
                NewPassword = "NewPassword123!"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Email không tồn tại trong hệ thống.");
            
            _userManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenResetFails()
        {
            // Arrange
            var user = new User { Email = "test@example.com", UserName = "testuser" };
            var command = new ResetPasswordCommand
            {
                Email = user.Email,
                Token = "invalid-token",
                NewPassword = "NewPassword123!"
            };
            var identityError = new IdentityError { Description = "Invalid token." };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage(identityError.Description);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessMessage_WhenResetSucceeds()
        {
            // Arrange
            var user = new User { Email = "test@example.com", UserName = "testuser" };
            var command = new ResetPasswordCommand
            {
                Email = user.Email,
                Token = "valid-token",
                NewPassword = "NewPassword123!"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be("Đặt lại mật khẩu thành công! Bạn có thể đăng nhập ngay.");
            _userManagerMock.Verify(x => x.ResetPasswordAsync(user, command.Token, command.NewPassword), Times.Once);
        }
    }
}
