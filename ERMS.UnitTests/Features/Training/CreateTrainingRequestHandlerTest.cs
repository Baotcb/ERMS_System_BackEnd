using ERMS.Application.Features.Training.Commands.CreateTrainingRequest;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AuldinSolutions.BackOffice;

namespace ERMS.UnitTests.Features.Training
{
    public class CreateTrainingRequestHandlerTest
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CreateTrainingRequestHandler>> _loggerMock;
        private readonly CreateTrainingRequestHandler _handler;

        public CreateTrainingRequestHandlerTest()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CreateTrainingRequestHandler>>();

            _handler = new CreateTrainingRequestHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldCreateTrainingRequest_WhenDataValid()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var departmentId = 1;
            var userId = Guid.NewGuid();

            var command = new CreateTrainingRequestCommand
            {
                Subject = "C# Training",
                Description = "Learn advanced C#"
            };

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)enterpriseId);

            _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync())
                .ReturnsAsync((int?)departmentId);

            // Mock Enterprises
            var enterprises = new List<Enterprise>
            {
                new Enterprise
                {
                    Id = enterpriseId,
                    IsDeleted = false
                }
            }.AsQueryable().BuildMockDbSet();

            // Mock Departments
            var departments = new List<Department>
            {
                new Department
                {
                    Id = departmentId,
                    EnterpriseId = enterpriseId,
                    IsDeleted = false
                }
            }.AsQueryable().BuildMockDbSet();

            // Mock TrainingRequests
            var trainingRequests = new List<TrainingRequest>()
                .AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.Enterprises)
                .Returns(enterprises.Object);

            _contextMock.Setup(x => x.Departments)
                .Returns(departments.Object);

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(trainingRequests.Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBe(Guid.Empty);

            _contextMock.Verify(
                x => x.TrainingRequests.Add(It.IsAny<TrainingRequest>()),
                Times.Once);

            _contextMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseNotExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var departmentId = 1;
            var userId = Guid.NewGuid();

            var command = new CreateTrainingRequestCommand
            {
                Subject = "Training"
            };

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)enterpriseId);

            _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync())
                .ReturnsAsync((int?)departmentId);

            // Enterprise rỗng
            var enterprises = new List<Enterprise>()
                .AsQueryable()
                .BuildMockDbSet();

            _contextMock.Setup(x => x.Enterprises)
                .Returns(enterprises.Object);

            // Act
            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Doanh nghiệp không tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserNotAuthenticated()
        {
            // Arrange
            var command = new CreateTrainingRequestCommand
            {
                Subject = "Test Training"
            };

            _currentUserServiceMock
                .Setup(x => x.UserId)
                .Returns((Guid?)null);

            // Act
            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User not authenticated");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDepartmentNotExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var departmentId = 1;
            var userId = Guid.NewGuid();

            var command = new CreateTrainingRequestCommand
            {
                Subject = "Training"
            };

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)enterpriseId);

            _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync())
                .ReturnsAsync((int?)departmentId);

            // Enterprise tồn tại
            var enterprises = new List<Enterprise>
    {
        new Enterprise
        {
            Id = enterpriseId,
            IsDeleted = false
        }
    }.AsQueryable().BuildMockDbSet();

            // Department rỗng
            var departments = new List<Department>()
                .AsQueryable()
                .BuildMockDbSet();

            _contextMock.Setup(x => x.Enterprises)
                .Returns(enterprises.Object);

            _contextMock.Setup(x => x.Departments)
                .Returns(departments.Object);

            // Act
            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Phòng ban không tồn tại");
        }
    }
}