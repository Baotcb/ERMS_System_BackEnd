using ERMS.Application.Features.Auth.Commands.ConfirmEmail;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.ConfirmEmail
{
    public class ConfirmEmailHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ILogger<ConfirmEmailHandler>> _loggerMock;
        private readonly ConfirmEmailHandler _handler;

        public ConfirmEmailHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _loggerMock = new Mock<ILogger<ConfirmEmailHandler>>();
            _handler = new ConfirmEmailHandler(_userManagerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenUserIdOrTokenIsMissing()
        {
            // Arrange
            var command = new ConfirmEmailCommand { UserId = "", Token = "" };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("UserId và Token là bắt buộc.");
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenUserNotFound()
        {
            // Arrange
            var command = new ConfirmEmailCommand { UserId = Guid.NewGuid().ToString(), Token = "token" };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("Tài khoản không tồn tại.");
        }

        [Fact]
        public async Task Handle_ShouldReturnMessage_WhenEmailAlreadyConfirmed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new ConfirmEmailCommand { UserId = userId.ToString(), Token = "token" };
            var user = new User { Id = userId, EmailConfirmed = true };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be("Email đã được xác thực trước đó. Bạn có thể đăng nhập bình thường.");
            _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccessMessage_WhenConfirmationSucceeds()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new ConfirmEmailCommand { UserId = userId.ToString(), Token = "token" };
            var user = new User { Id = userId, Email = "test@example.com", EmailConfirmed = false };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be("🎉 Email đã được xác thực thành công! Bây giờ bạn có thể đăng nhập vào hệ thống.");
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenConfirmationFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new ConfirmEmailCommand { UserId = userId.ToString(), Token = "token" };
            var user = new User { Id = userId, EmailConfirmed = false };
            var identityError = new IdentityError { Description = "Invalid token" };

            _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, command.Token))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage($"Xác thực email thất bại: {identityError.Description}. Có thể token đã hết hạn hoặc không hợp lệ.");
        }
    }
}
