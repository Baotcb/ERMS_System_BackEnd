using ERMS.Application.Features.Users.Commands.GetProfile;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Enterprise;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Users.Commands.GetProfile
{
    public class GetProfileHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly GetProfileHandler _handler;

        public GetProfileHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new GetProfileHandler(_userManagerMock.Object, _currentUserServiceMock.Object, _contextMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new GetProfileCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Không tìm thấy thông tin người dùng trong Token.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            // Mock Users property using BuildMockDbSet
            var users = new List<User>().AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(x => x.Users).Returns(users.Object);

            var command = new GetProfileCommand();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Người dùng không tồn tại trong hệ thống.");
        }

        [Fact]
        public async Task Handle_ShouldReturnProfile_WhenUserFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterprise = new Enterprise { EnterpriseName = "Test Corp", LogoUrl = "https://logo.png" };
            var department = new Department { DepartmentName = "HR", Enterprise = enterprise };
            var user = new User
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com",
                FullName = "Test User",
                PhoneNumber = "0987654321",
                AvatarUrl = "https://res.cloudinary.com/demo/image/upload/avatar.png",
                DateJoined = DateTime.UtcNow,
                Department = department,
                DepartmentId = 1
            };

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            // Mock Users property using BuildMockDbSet (supports Include)
            var mockUsers = new List<User> { user }.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(x => x.Users).Returns(mockUsers.Object);

            // Mock Employees DbSet (handler uses fallback query)
            var mockEmployees = new List<Employee>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(mockEmployees.Object);

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Manager" });

            var command = new GetProfileCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UserName.Should().Be(user.UserName);
            result.FullName.Should().Be(user.FullName);
            result.Phones.Should().Be(user.PhoneNumber);
            result.AvatarUrl.Should().Be(user.AvatarUrl);
            result.DepartmentName.Should().Be("HR");
        }
    }
}

