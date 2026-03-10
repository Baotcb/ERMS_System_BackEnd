using ERMS.Application.Features.Auth.Commands.ResendConfirmation;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.ResendConfirmation
{
    public class ResendConfirmationHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<ResendConfirmationHandler>> _loggerMock;
        private readonly ResendConfirmationHandler _handler;

        public ResendConfirmationHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _emailServiceMock = new Mock<IEmailService>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<ResendConfirmationHandler>>();

            _handler = new ResendConfirmationHandler(
                _userManagerMock.Object,
                _emailServiceMock.Object,
                _configMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenUserNotFound()
        {
            // Arrange
            var command = new ResendConfirmationCommand { Email = "notfound@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email không tồn tại trong hệ thống.");
        }

        [Fact]
        public async Task Handle_ShouldThrowInvalidOperationException_WhenEmailAlreadyConfirmed()
        {
            // Arrange
            var user = new User { Email = "test@example.com", EmailConfirmed = true };
            var command = new ResendConfirmationCommand { Email = user.Email };
            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email đã được xác thực trước đó.");
        }

        [Fact]
        public async Task Handle_ShouldSendEmail_WhenUserExistsAndNotConfirmed()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = false, FullName = "Test User" };
            var command = new ResendConfirmationCommand { Email = user.Email };
            var token = "confirmation-token";
            var clientUrl = "http://localhost:3000";

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync(token);
            _configMock.Setup(x => x["ClientSettings:Url"]).Returns(clientUrl);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendEmailAsync(user.Email, "Xác thực email tài khoản ERMS", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenClientUrlIsInvalid()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = false, FullName = "Test User" };
            var command = new ResendConfirmationCommand { Email = user.Email };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _configMock.Setup(x => x["ClientSettings:Url"]).Returns("not-a-valid-url");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            _userManagerMock.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenEmailServiceThrows()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = false, FullName = "Test User" };
            var command = new ResendConfirmationCommand { Email = user.Email };
            var token = "confirmation-token";
            var clientUrl = "http://localhost:3000";

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync(token);
            _configMock.Setup(x => x["ClientSettings:Url"]).Returns(clientUrl);
            _emailServiceMock.Setup(x => x.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }
    }
}
