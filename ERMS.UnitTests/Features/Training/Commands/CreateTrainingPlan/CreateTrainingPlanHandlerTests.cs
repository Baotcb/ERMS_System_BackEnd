using ERMS.Application.Features.Training.Commands.CreateTrainingPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.CreateTrainingPlan
{
    public class CreateTrainingPlanHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CreateTrainingPlanHandler>> _loggerMock;
        private readonly Mock<IDbContextTransaction> _transactionMock;

        private readonly CreateTrainingPlanHandler _handler;

        public CreateTrainingPlanHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CreateTrainingPlanHandler>>();
            _transactionMock = new Mock<IDbContextTransaction>();

            _handler = new CreateTrainingPlanHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private void SetupPlans(List<TrainingPlan> plans)
        {
            var dbSetMock = plans.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(dbSetMock.Object);
        }

        private void SetupRequests(List<TrainingRequest> requests)
        {
            var dbSetMock = requests.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_UserNotAuthenticated_ThrowsException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns((Guid?)null);

            var command = new CreateTrainingPlanCommand();

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User not authenticated");
        }

        [Fact]
        public async Task Handle_UserWithoutEnterprise_ThrowsException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x =>
                x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var command = new CreateTrainingPlanCommand();

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User does not belong to any enterprise");
        }

        [Fact]
        public async Task Handle_EndDateBeforeStartDate_ThrowsException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x =>
                x.GetEnterpriseIdAsync())
                .ReturnsAsync(Guid.NewGuid());

            var command = new CreateTrainingPlanCommand
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-1),
                TrainingRequestIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("EndDate must be greater than StartDate");
        }

        [Fact]
        public async Task Handle_NoTrainingRequests_ThrowsException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x =>
                x.GetEnterpriseIdAsync())
                .ReturnsAsync(Guid.NewGuid());

            var command = new CreateTrainingPlanCommand
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Training requests are required");
        }

        [Fact]
        public async Task Handle_DuplicatePlanCode_ThrowsException()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x =>
                x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>
            {
                new TrainingPlan
                {
                    PlanCode = "PLAN001",
                    EnterpriseId = enterpriseId,
                    IsDeleted = false
                }
            });

            var command = new CreateTrainingPlanCommand
            {
                PlanCode = "PLAN001",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                TrainingRequestIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("PlanCode already exists");
        }

        [Fact]
        public async Task Handle_InvalidTrainingRequests_ThrowsException()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var reqId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x =>
                x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>());

            SetupRequests(new List<TrainingRequest>());

            var command = new CreateTrainingPlanCommand
            {
                PlanCode = "PLAN002",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                TrainingRequestIds = new List<Guid> { reqId }
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Some training requests are invalid");
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesTrainingPlan()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            _currentUserServiceMock.Setup(x =>
                x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>());

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = requestId,
                    Status = "Pending",
                    TrainingPlanId = null,
                    IsDeleted = false
                }
            };

            SetupRequests(requests);

            _contextMock.Setup(x =>
                x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_transactionMock.Object);

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateTrainingPlanCommand
            {
                PlanName = "Training Plan",
                PlanCode = "PLAN003",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                TrainingRequestIds = new List<Guid> { requestId }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();

            requests[0].TrainingPlanId.Should().NotBeNull();
            requests[0].Status.Should().Be("AddedToPlan");

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _transactionMock.Verify(x =>
                x.CommitAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}