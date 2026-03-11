using ERMS.Application.Features.Training.Commands.ApproveTrainingPlan;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.Training.Commands.ApproveTrainingPlan
{
    public class ApproveTrainingPlanHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<ApproveTrainingPlanHandler>> _loggerMock;

        private readonly ApproveTrainingPlanHandler _handler;

        public ApproveTrainingPlanHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<ApproveTrainingPlanHandler>>();

            _handler = new ApproveTrainingPlanHandler(
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

        [Fact]
        public async Task Handle_UserNotLoggedIn_ShouldThrowUnauthorizedAccessException()
        {
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns((Guid?)null);

            var command = new ApproveTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_PlanNotFound_ShouldThrowException()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>());

            var command = new ApproveTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch đào tạo");
        }

        [Fact]
        public async Task Handle_AlreadyApproved_ShouldThrowException()
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
                    Status = "Approved",
                    IsDeleted = false
                }
            });

            var command = new ApproveTrainingPlanCommand
            {
                TrainingPlanId = planId
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Kế hoạch đã được phê duyệt");
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldApproveTrainingPlan()
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
                Status = "Draft",
                IsDeleted = false
            };

            SetupPlans(new List<TrainingPlan> { plan });

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new ApproveTrainingPlanCommand
            {
                TrainingPlanId = planId,
                ReviewNote = "Approved by HR"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            plan.Status.Should().Be("Approved");
            plan.ApprovedById.Should().Be(userId);
            plan.ReviewNote.Should().Be("Approved by HR");
            plan.ApprovedAt.Should().NotBeNull();

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}