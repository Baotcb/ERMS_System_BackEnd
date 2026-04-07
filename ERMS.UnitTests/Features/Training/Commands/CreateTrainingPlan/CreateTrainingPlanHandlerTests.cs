using ERMS.Application.Features.Training.Commands.CreateTrainingPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.CreateTrainingPlan
{
    public class CreateTrainingPlanHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<CreateTrainingPlanHandler>> _loggerMock = new();
        private readonly Mock<ISubscriptionLimitChecker> _subscriptionLimitCheckerMock = new();

        private readonly CreateTrainingPlanHandler _handler;

        public CreateTrainingPlanHandlerTests()
        {
            _subscriptionLimitCheckerMock.Setup(x => x.IsProPlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _handler = new CreateTrainingPlanHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object,
                _subscriptionLimitCheckerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new CreateTrainingPlanCommand();

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenEndDateBeforeStartDate()
        {
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(Guid.NewGuid());

            var command = new CreateTrainingPlanCommand
            {
                PlanName = "Plan",
                PlanCode = "PLAN01",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-1),
                TrainingRequestIds = new List<Guid> { Guid.NewGuid() }
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenNoTrainingRequests()
        {
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(Guid.NewGuid());

            var command = new CreateTrainingPlanCommand
            {
                PlanName = "Plan",
                PlanCode = "PLAN01",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5)
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Cần có ít nhất một yêu cầu đào tạo");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenPlanCodeExists()
        {
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var plans = new List<TrainingPlan>
            {
                new TrainingPlan
                {
                    Id = Guid.NewGuid(),
                    PlanCode = "PLAN01",
                    EnterpriseId = enterpriseId,
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(plans.AsQueryable().BuildMockDbSet().Object);

            var command = new CreateTrainingPlanCommand
            {
                PlanName = "Plan",
                PlanCode = "PLAN01",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                TrainingRequestIds = new List<Guid> { Guid.NewGuid() }
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Mã kế hoạch đã tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenTrainingRequestsInvalid()
        {
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(new List<TrainingPlan>()
                .AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(new List<TrainingRequest>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new CreateTrainingPlanCommand
            {
                PlanName = "Plan",
                PlanCode = "PLAN01",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                TrainingRequestIds = new List<Guid> { Guid.NewGuid() }
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Một số yêu cầu đào tạo không hợp lệ");
        }

        [Fact]
        public async Task Handle_ShouldCreateTrainingPlanSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var trainingRequests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = requestId,
                    Status = "Pending",
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(new List<TrainingPlan>()
                .AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(trainingRequests.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<IDbContextTransaction>());

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateTrainingPlanCommand
            {
                PlanName = "Training Plan 2026",
                PlanCode = "PLAN2026",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(10),
                TrainingRequestIds = new List<Guid> { requestId }
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();

            trainingRequests[0].Status.Should().Be("AddedToPlan");
        }
    }
}