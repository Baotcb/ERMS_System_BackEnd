using ERMS.Application.Features.Auth.Commands.Register;
using ERMS.Application.Features.Auth.Commands.ResendConfirmation;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Identity;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Auth.Command.Register
{
    public class RegisterHandlerTest
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<DbSet<Candidate>> _candidatesDbSetMock;
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
            _mediatorMock = new Mock<IMediator>();

            _candidatesDbSetMock = new Mock<DbSet<Candidate>>();
            _contextMock.Setup(x => x.Candidates).Returns(_candidatesDbSetMock.Object);

            _handler = new RegisterHandler(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _contextMock.Object,
                _mediatorMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEmailExists()
        {
            // Arrange
            var command = new RegisterCommand { Email = "existing@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(new User());

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Email đã tồn tại trong hệ thống.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCreateUserFails()
        {
            // Arrange
            var command = new RegisterCommand { Email = "new@example.com", Password = "Password123!", FullName = "Test User" };
            var identityError = new IdentityError { Description = "Weak password" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage($"Đăng ký không thành công: {identityError.Description}");
        }

        [Fact]
        public async Task Handle_ShouldCreateUserAndCandidate_WhenSuccessful()
        {
            // Arrange
            var command = new RegisterCommand { Email = "success@example.com", Password = "Password123!", FullName = "Test User" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync((User)null!);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), command.Password))
                .Callback<User, string>((u, p) => u.Id = Guid.NewGuid()) 
                .ReturnsAsync(IdentityResult.Success);
            
            _roleManagerMock.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            //result.Should().NotBeEmpty();
            _candidatesDbSetMock.Verify(x => x.Add(It.IsAny<Candidate>()), Times.Once);
            _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(x => x.Send(It.IsAny<ResendConfirmationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
