using ERMS.Application.Features.Training.Commands.CloseTrainingPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Training;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.CloseTrainingPlan
{
    public class CloseTrainingPlanHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CloseTrainingPlanHandler>> _loggerMock;

        private readonly CloseTrainingPlanHandler _handler;

        public CloseTrainingPlanHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CloseTrainingPlanHandler>>();

            _handler = new CloseTrainingPlanHandler(
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

        //   User chưa login
        [Fact]
        public async Task Handle_UserNotLoggedIn_ShouldThrowUnauthorizedAccessException()
        {
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns((Guid?)null);

            var command = new CloseTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        //   Plan không tồn tại
        [Fact]
        public async Task Handle_PlanNotFound_ShouldThrowException()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>());

            var command = new CloseTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch đào tạo");
        }

        //   Plan đã closed
        [Fact]
        public async Task Handle_AlreadyClosed_ShouldThrowException()
        {
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>
            {
                new TrainingPlan
                {
                    Id = planId,
                    EnterpriseId = enterpriseId,
                    Status = "Closed",
                    IsDeleted = false
                }
            });

            var command = new CloseTrainingPlanCommand
            {
                TrainingPlanId = planId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Kế hoạch đã được đóng");
        }

        //   Plan chưa approved
        [Fact]
        public async Task Handle_NotApproved_ShouldThrowException()
        {
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>
            {
                new TrainingPlan
                {
                    Id = planId,
                    EnterpriseId = enterpriseId,
                    Status = "Draft",
                    IsDeleted = false
                }
            });

            var command = new CloseTrainingPlanCommand
            {
                TrainingPlanId = planId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Chỉ có thể đóng kế hoạch đã được phê duyệt");
        }

        //   Happy path
        [Fact]
        public async Task Handle_ValidRequest_ShouldClosePlan_AndCompleteRequests()
        {
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var plan = new TrainingPlan
            {
                Id = planId,
                EnterpriseId = enterpriseId,
                Status = "Approved",
                IsDeleted = false
            };

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    TrainingPlanId = planId,
                    Status = TrainingRequestStatus.Pending,
                    IsDeleted = false
                },
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    TrainingPlanId = planId,
                    Status = TrainingRequestStatus.Completed,
                    IsDeleted = false
                }
            };

            SetupPlans(new List<TrainingPlan> { plan });
            SetupRequests(requests);

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CloseTrainingPlanCommand
            {
                TrainingPlanId = planId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            //  Plan closed
            plan.Status.Should().Be("Closed");

            //  Requests updated
            requests[0].Status.Should().Be(TrainingRequestStatus.Completed);
            requests[0].UpdatedAt.Should().NotBeNull();

            // request đã completed thì giữ nguyên
            requests[1].Status.Should().Be(TrainingRequestStatus.Completed);

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}