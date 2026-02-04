using ERMS.Application.Features.Auth.Commands.Login;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.Login
{
    public class LoginHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly LoginHandler _handler;

        public LoginHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _tokenServiceMock = new Mock<ITokenService>();
            _handler = new LoginHandler(_userManagerMock.Object, _tokenServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var command = new LoginCommand { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Email = "test@example.com", UserName = "testuser" };
            var token = "jwt-token-example";

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
                .ReturnsAsync(true);
            _tokenServiceMock.Setup(x => x.CreateToken(user))
                .ReturnsAsync(token);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(token);
            _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(user, command.Password), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var command = new LoginCommand { Email = "test@example.com", Password = "Password123!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tài khoản hoặc mật khẩu không đúng.");

            _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPasswordIsInvalid()
        {
            // Arrange
            var command = new LoginCommand { Email = "test@example.com", Password = "WrongPassword" };
            var user = new User { Email = "test@example.com", UserName = "testuser" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password))
                .ReturnsAsync(false);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tài khoản hoặc mật khẩu không đúng.");

            _userManagerMock.Verify(x => x.FindByEmailAsync(command.Email), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(user, command.Password), Times.Once);
            _tokenServiceMock.Verify(x => x.CreateToken(It.IsAny<User>()), Times.Never);
        }
    }
}
