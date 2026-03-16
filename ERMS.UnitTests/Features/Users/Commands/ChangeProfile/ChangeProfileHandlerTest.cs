using ERMS.Application.Features.Users.Commands.ChangeProfile;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Users.Commands.ChangeProfile
{
    public class ChangeProfileHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly ChangeProfileHandler _handler;

        public ChangeProfileHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new ChangeProfileHandler(_userManagerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new ChangeProfileCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Không tìm thấy thông tin người dùng trong Token.");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User)null!);

            var command = new ChangeProfileCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không tồn tại.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUpdateFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            var identityError = new IdentityError { Description = "Update error" };
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            var command = new ChangeProfileCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Cập nhật profile thất bại: {identityError.Description}");
        }

        [Fact]
        public async Task Handle_ShouldUpdateProfileAndReturnDto_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com",
                Department = new Department { DepartmentName = "IT" }
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var command = new ChangeProfileCommand
            {
                FullName = "Updated Name",
                DateOfBirth = new DateTime(1995, 5, 5),
                Hometown = "New City",
                Phones = "0987654321",
                Address = "123 Street",
                AvatarUrl = "https://res.cloudinary.com/demo/image/upload/avatar.png"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be(command.FullName);
            result.DateOfBirth.Should().Be(command.DateOfBirth);
            result.Hometown.Should().Be(command.Hometown);
            result.Phones.Should().Be(command.Phones);
            result.AvatarUrl.Should().Be(command.AvatarUrl);
            result.DepartmentName.Should().Be("IT");

            user.FullName.Should().Be(command.FullName);
            user.DateOfBirth.Should().Be(command.DateOfBirth);
            user.Hometown.Should().Be(command.Hometown);
            user.PhoneNumber.Should().Be(command.Phones);
            user.AvatarUrl.Should().Be(command.AvatarUrl);
        }

        [Fact]
        public async Task Handle_ShouldClearAvatar_WhenAvatarUrlIsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                AvatarUrl = "https://res.cloudinary.com/demo/image/upload/old-avatar.png"
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var command = new ChangeProfileCommand
            {
                FullName = "Updated Name",
                AvatarUrl = null
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.AvatarUrl.Should().BeNull();
            user.AvatarUrl.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenAvatarUrlIsInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            var command = new ChangeProfileCommand
            {
                FullName = "Updated Name",
                AvatarUrl = "not-a-valid-url"
            };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Avatar URL không hợp lệ.");
        }
    }
}
