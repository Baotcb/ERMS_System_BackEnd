using ERMS.Application.Features.Auth.Commands.GoogleLogin;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.GoogleLogin
{
    public class GoogleLoginHandlerTest
    {
        private Mock<UserManager<User>> _userManagerMock;
        private Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private Mock<ITokenService> _tokenServiceMock;
        private Mock<IGoogleAuthService> _googleAuthServiceMock;
        private ERMSDbContext _context;
        private GoogleLoginHandler _handler;

        public GoogleLoginHandlerTest()
        {
            var userStoreMock = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStoreMock.Object, null, null, null, null);

            _tokenServiceMock = new Mock<ITokenService>();
            _googleAuthServiceMock = new Mock<IGoogleAuthService>();

            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ERMSDbContext(options);

            _handler = new GoogleLoginHandler(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _tokenServiceMock.Object,
                _googleAuthServiceMock.Object,
                _context);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEmailNotVerified()
        {
            // Arrange
            var request = new GoogleLoginCommand { IdToken = "valid-token" };
            var payload = new GoogleUserInfo { EmailVerified = false };

            _googleAuthServiceMock.Setup(x => x.ValidateIdTokenAsync(request.IdToken))
                .ReturnsAsync(payload);

            // Act & Assert
            await _handler.Invoking(h => h.Handle(request, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Email Google chýa ðý?c xác th?c.");
        }

        [Fact]
        public async Task Handle_ShouldReturnToken_WhenUserExistsByLogin()
        {
            // Arrange
            var request = new GoogleLoginCommand { IdToken = "valid-token" };
            var payload = new GoogleUserInfo 
            { 
                EmailVerified = true, 
                Email = "test@example.com", 
                Subject = "google-sub-123" 
            };
            var user = new User { Id = Guid.NewGuid(), Email = payload.Email };

            _googleAuthServiceMock.Setup(x => x.ValidateIdTokenAsync(request.IdToken))
                .ReturnsAsync(payload);

            _userManagerMock.Setup(x => x.FindByLoginAsync("Google", payload.Subject))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { AppRoles.Candidate });

            _tokenServiceMock.Setup(x => x.CreateToken(user))
                .ReturnsAsync("jwt-token");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().Be("jwt-token");
        }

        [Fact]
        public async Task Handle_ShouldLinkAndReturnToken_WhenUserExistsByEmail()
        {
            // Arrange
            var request = new GoogleLoginCommand { IdToken = "valid-token" };
            var payload = new GoogleUserInfo 
            { 
                EmailVerified = true, 
                Email = "test@example.com", 
                Subject = "google-sub-123" 
            };
            var user = new User { Id = Guid.NewGuid(), Email = payload.Email };

            _googleAuthServiceMock.Setup(x => x.ValidateIdTokenAsync(request.IdToken))
                .ReturnsAsync(payload);

            _userManagerMock.Setup(x => x.FindByLoginAsync("Google", payload.Subject))
                .ReturnsAsync((User)null);

            _userManagerMock.Setup(x => x.FindByEmailAsync(payload.Email))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.AddLoginAsync(user, It.IsAny<UserLoginInfo>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { AppRoles.Candidate });

            _tokenServiceMock.Setup(x => x.CreateToken(user))
                .ReturnsAsync("jwt-token");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().Be("jwt-token");
            _userManagerMock.Verify(x => x.AddLoginAsync(user, It.Is<UserLoginInfo>(l => l.LoginProvider == "Google" && l.ProviderKey == payload.Subject)), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCreateNewUserAndCandidate_WhenUserDoesNotExist()
        {
            // Arrange
            var request = new GoogleLoginCommand { IdToken = "valid-token" };
            var payload = new GoogleUserInfo 
            { 
                EmailVerified = true, 
                Email = "new@example.com", 
                Subject = "google-sub-456",
                Name = "New User",
                Picture = "http://avatar.url"
            };

            _googleAuthServiceMock.Setup(x => x.ValidateIdTokenAsync(request.IdToken))
                .ReturnsAsync(payload);

            _userManagerMock.Setup(x => x.FindByLoginAsync("Google", payload.Subject))
                .ReturnsAsync((User)null);

            _userManagerMock.Setup(x => x.FindByEmailAsync(payload.Email))
                .ReturnsAsync((User)null);

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<User>(u => u.Id = Guid.NewGuid());

            _userManagerMock.Setup(x => x.AddLoginAsync(It.IsAny<User>(), It.IsAny<UserLoginInfo>()))
                .ReturnsAsync(IdentityResult.Success);

            _roleManagerMock.Setup(x => x.RoleExistsAsync(AppRoles.Candidate))
                .ReturnsAsync(true);

            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Candidate))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { AppRoles.Candidate });

            _tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<User>()))
                .ReturnsAsync("jwt-token");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().Be("jwt-token");
            _context.Candidates.Count().Should().Be(1);
            _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u => u.Email == payload.Email && u.FullName == payload.Name)), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseIsLocked()
        {
            // Arrange
            var request = new GoogleLoginCommand { IdToken = "valid-token" };
            var payload = new GoogleUserInfo 
            { 
                EmailVerified = true, 
                Email = "hr@enterprise.com", 
                Subject = "google-sub-789" 
            };
            var user = new User { Id = Guid.NewGuid(), Email = payload.Email };
            var enterpriseId = Guid.NewGuid();

            _googleAuthServiceMock.Setup(x => x.ValidateIdTokenAsync(request.IdToken))
                .ReturnsAsync(payload);

            _userManagerMock.Setup(x => x.FindByLoginAsync("Google", payload.Subject))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { AppRoles.HRManager });

            // Setup data in InMemory DB
            _context.Employees.Add(new Employee 
            { 
                Id = Guid.NewGuid(),
                UserId = user.Id, 
                EnterpriseId = enterpriseId,
                EmployeeCode = "EMP001"
            });
            _context.Enterprises.Add(new Enterprise 
            { 
                Id = enterpriseId, 
                Status = "Locked",
                EnterpriseName = "Test Enterprise",
                EnterpriseCode = "ENT001"
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(request, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Tài kho?n doanh nghi?p ð? b? khóa. Vui l?ng liên h? qu?n tr? viên ð? bi?t thêm chi ti?t.");
        }
    }
}
