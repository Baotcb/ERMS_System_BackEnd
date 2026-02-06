using ERMS.Application.Features.Auth.Commands.Login;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Enterprise;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly LoginHandler _handler;

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return dbSetMock;
        }

        public LoginHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _tokenServiceMock = new Mock<ITokenService>();
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new LoginHandler(_userManagerMock.Object, _tokenServiceMock.Object, _contextMock.Object);
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
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Candidate" });

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
        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseIsLocked()
        {
            // Arrange
            var command = new LoginCommand { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };
            var enterpriseId = Guid.NewGuid();
            var employee = new Employee { UserId = user.Id, EnterpriseId = enterpriseId };
            var enterprise = new Enterprise { Id = enterpriseId, Status = ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Locked };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employer" });

            var employeesMock = CreateMockDbSet(new List<Employee> { employee });
            var enterprisesMock = CreateMockDbSet(new List<Enterprise> { enterprise });
            enterprisesMock.Setup(x => x.Find(enterpriseId)).Returns(enterprise);

            _contextMock.Setup(x => x.Employees).Returns(employeesMock.Object);
            _contextMock.Setup(x => x.Enterprises).Returns(enterprisesMock.Object);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tài khoản doanh nghiệp đã bị khóa. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseIsSuspended()
        {
            // Arrange
            var command = new LoginCommand { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };
            var enterpriseId = Guid.NewGuid();
            var employee = new Employee { UserId = user.Id, EnterpriseId = enterpriseId };
            var enterprise = new Enterprise { Id = enterpriseId, Status = ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Suspended };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employer" });

            var employeesMock = CreateMockDbSet(new List<Employee> { employee });
            var enterprisesMock = CreateMockDbSet(new List<Enterprise> { enterprise });
            enterprisesMock.Setup(x => x.Find(enterpriseId)).Returns(enterprise);

            _contextMock.Setup(x => x.Employees).Returns(employeesMock.Object);
            _contextMock.Setup(x => x.Enterprises).Returns(enterprisesMock.Object);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tài khoản doanh nghiệp của bạn đang chờ phê duyệt. Vui lòng chờ quản trị viên phê duyệt tài khoản của bạn.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseIsInactive()
        {
            // Arrange
            var command = new LoginCommand { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };
            var enterpriseId = Guid.NewGuid();
            var employee = new Employee { UserId = user.Id, EnterpriseId = enterpriseId };
            var enterprise = new Enterprise { Id = enterpriseId, Status = ERMS.Domain.Constants.Enterprise.EnterpriseStatus.Inactive };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employer" });

            var employeesMock = CreateMockDbSet(new List<Employee> { employee });
            var enterprisesMock = CreateMockDbSet(new List<Enterprise> { enterprise });
            enterprisesMock.Setup(x => x.Find(enterpriseId)).Returns(enterprise);

            _contextMock.Setup(x => x.Employees).Returns(employeesMock.Object);
            _contextMock.Setup(x => x.Enterprises).Returns(enterprisesMock.Object);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tài khoản doanh nghiệp không hoạt động. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
        }

        [Fact]
        public async Task Handle_ShouldReturnToken_WhenUserIsAdmin_EvenIfEnterpriseStatusIsIssues()
        {
            // Arrange
            var command = new LoginCommand { Email = "admin@example.com", Password = "Password123!" };
            var user = new User { Id = Guid.NewGuid(), Email = "admin@example.com", UserName = "admin" };
            var token = "admin-token";

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { AppRoles.Admin });
            _tokenServiceMock.Setup(x => x.CreateToken(user)).ReturnsAsync(token);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(token);
            _contextMock.Verify(x => x.Employees, Times.Never);
            _contextMock.Verify(x => x.Enterprises.Find(It.IsAny<int>()), Times.Never);
        }
    }
}
