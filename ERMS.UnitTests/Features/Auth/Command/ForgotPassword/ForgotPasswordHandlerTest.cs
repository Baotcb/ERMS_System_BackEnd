using ERMS.Application.Features.Auth.Commands.ForgotPassword;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.ForgotPassword
{
    public class ForgotPasswordHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly ForgotPasswordHandler _handler;

        public ForgotPasswordHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _handler = new ForgotPasswordHandler(_userManagerMock.Object, _emailServiceMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFakeSuccess_WhenUserNotFound()
        {
            // Arrange
            var command = new ForgotPasswordCommand { Email = "notfound@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be("Email đã được gửi!");
            _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<User>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldSendEmail_WhenUserExists()
        {
            // Arrange
            var user = new User { Email = "test@example.com", FullName = "Test User" };
            var command = new ForgotPasswordCommand { Email = user.Email };
            var token = "reset-token";
            var clientUrl = "http://localhost:3000";

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);
            _configurationMock.Setup(x => x["ClientSettings:Url"]).Returns(clientUrl);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be("Email đã được gửi!");
            _emailServiceMock.Verify(x => x.SendEmailAsync(user.Email, "Reset Password", It.IsAny<string>()), Times.Once);
        }
    }
}
