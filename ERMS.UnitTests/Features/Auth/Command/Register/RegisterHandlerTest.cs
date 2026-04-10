using ERMS.Application.Features.Auth.Commands.Register;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.Register
{
    public class RegisterHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<DbSet<Candidate>> _candidatesDbSetMock;
        private readonly Mock<ILogger<RegisterHandler>> _loggerMock;
        private readonly RegisterHandler _handler;

        public RegisterHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStoreMock.Object, null, null, null, null);

            _contextMock = new Mock<IERMSDbContext>();
            _loggerMock = new Mock<ILogger<RegisterHandler>>();

            _candidatesDbSetMock = new Mock<DbSet<Candidate>>();
            _contextMock.Setup(x => x.Candidates).Returns(_candidatesDbSetMock.Object);

            _handler = new RegisterHandler(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _contextMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEmailExists()
        {
            var command = new RegisterCommand { Email = "existing@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(new User());

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Email đã tồn tại trong hệ thống.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCreateUserFails()
        {
            var command = new RegisterCommand { Email = "new@example.com", Password = "Password123!", FullName = "Test User" };
            var identityError = new IdentityError { Description = "Weak password" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Đăng ký không thành công: {identityError.Description}");
        }

        [Fact]
        public async Task Handle_ShouldCreateUserAndCandidate_WhenSuccessful()
        {
            var command = new RegisterCommand { Email = "success@example.com", Password = "Password123!", FullName = "Test User" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .Callback<User, string>((user, _) => user.Id = Guid.NewGuid())
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(AppRoles.Candidate))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Candidate))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();
            _candidatesDbSetMock.Verify(x => x.Add(It.IsAny<Candidate>()), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldDeleteCreatedUser_WhenAddToRoleFails()
        {
            var command = new RegisterCommand { Email = "cleanup@example.com", Password = "Password123!", FullName = "Test User" };
            var createdUserId = Guid.NewGuid();
            var identityError = new IdentityError { Description = "Role assignment failed" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .Callback<User, string>((user, _) => user.Id = createdUserId)
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(AppRoles.Candidate))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Candidate))
                .ReturnsAsync(IdentityResult.Failed(identityError));
            _userManagerMock.Setup(x => x.DeleteAsync(It.Is<User>(u => u.Id == createdUserId)))
                .ReturnsAsync(IdentityResult.Success);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không thể gán vai trò ứng viên: Role assignment failed");

            _userManagerMock.Verify(x => x.DeleteAsync(It.Is<User>(u => u.Id == createdUserId)), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldDeleteCreatedUser_WhenCandidateCreationFails()
        {
            var command = new RegisterCommand { Email = "candidatefail@example.com", Password = "Password123!", FullName = "Test User" };
            var createdUserId = Guid.NewGuid();

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .Callback<User, string>((user, _) => user.Id = createdUserId)
                .ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(x => x.RoleExistsAsync(AppRoles.Candidate))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Candidate))
                .ReturnsAsync(IdentityResult.Success);
            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database failed"));
            _userManagerMock.Setup(x => x.DeleteAsync(It.Is<User>(u => u.Id == createdUserId)))
                .ReturnsAsync(IdentityResult.Success);

            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Database failed");

            _userManagerMock.Verify(x => x.DeleteAsync(It.Is<User>(u => u.Id == createdUserId)), Times.Once);
        }
    }
}
