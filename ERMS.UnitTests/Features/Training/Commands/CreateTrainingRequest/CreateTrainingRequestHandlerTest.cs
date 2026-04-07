using ERMS.Application.Features.Training.Commands.CreateTrainingRequest;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization; // Đảm bảo namespace chứa Enterprise/Department
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.CreateTrainingRequest
{
    public class CreateTrainingRequestHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<CreateTrainingRequestHandler>> _loggerMock = new();
        private readonly CreateTrainingRequestHandler _handler;

        public CreateTrainingRequestHandlerTests()
        {
            _handler = new CreateTrainingRequestHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateTrainingRequestSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            int departmentId = 101; // Sửa thành int để khớp với Interface ICurrentUserService

            // Setup User Service
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
            _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(departmentId);

            // Mock Enterprise (Bắt buộc phải có để vượt qua bước Validate)
            var enterprises = new List<Enterprise>
            {
                new Enterprise { Id = enterpriseId, IsDeleted = false }
            }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Enterprises).Returns(enterprises.Object);

            // Mock Department (Lưu ý: Id ở đây cũng phải là int theo logic của bạn)
            var departments = new List<Department>
            {
                new Department { Id = departmentId, EnterpriseId = enterpriseId, IsDeleted = false }
            }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Departments).Returns(departments.Object);

            // Mock TrainingRequests DbSet
            var trainingRequests = new List<TrainingRequest>().AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingRequests).Returns(trainingRequests.Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateTrainingRequestCommand
            {
                Subject = "Kỹ năng lập trình .NET 2026",
                Urgency = "High",
                Description = "Đào tạo nâng cao",
                TargetAudience = "Developer",
                EstimatedParticipants = 10,
                EstimatedBudget = 1000000
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Created training request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new CreateTrainingRequestCommand();

            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
        [Fact]
        public async Task Handle_ShouldThrow_WhenEnterpriseIsDeleted()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            // Setup Enterprise với IsDeleted = true
            var enterprises = new List<Enterprise>
    {
        new Enterprise { Id = enterpriseId, IsDeleted = true }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Enterprises).Returns(enterprises.Object);

            var command = new CreateTrainingRequestCommand { Subject = "Test" };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Doanh nghiệp không tồn tại");
        }
        [Fact]
        public async Task Handle_ShouldThrow_WhenDepartmentBelongsToAnotherEnterprise()
        {
            // Arrange
            var myEnterpriseId = Guid.NewGuid();
            var otherEnterpriseId = Guid.NewGuid();
            int departmentId = 999;

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(myEnterpriseId);
            _currentUserServiceMock.Setup(x => x.GetDepartmentIdAsync()).ReturnsAsync(departmentId);

            _contextMock.Setup(x => x.Enterprises).Returns(new List<Enterprise>
        { new Enterprise { Id = myEnterpriseId, IsDeleted = false } }.AsQueryable().BuildMockDbSet().Object);

            // Setup Department nhưng EnterpriseId không khớp
            var departments = new List<Department>
    {
        new Department { Id = departmentId, EnterpriseId = otherEnterpriseId, IsDeleted = false }
    }.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Departments).Returns(departments.Object);

            var command = new CreateTrainingRequestCommand { Subject = "Test" };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Phòng ban không tồn tại");
        }
    }
}