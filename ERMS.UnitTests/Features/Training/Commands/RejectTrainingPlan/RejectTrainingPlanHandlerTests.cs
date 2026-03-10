using ERMS.Application.Features.Training.Commands.RejectTrainingPlan;
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

namespace ERMS.UnitTests.Features.Training.Commands.RejectTrainingPlan
{
    public class RejectTrainingPlanHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<RejectTrainingPlanHandler>> _loggerMock;

        private readonly RejectTrainingPlanHandler _handler;

        public RejectTrainingPlanHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<RejectTrainingPlanHandler>>();

            _handler = new RejectTrainingPlanHandler(
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
        public async Task Handle_UserNotLoggedIn_ThrowsUnauthorizedAccessException()
        {
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns((Guid?)null);

            var command = new RejectTrainingPlanCommand();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_PlanNotFound_ThrowsException()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupPlans(new List<TrainingPlan>());

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid(),
                ReviewNote = "Reject reason"
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Training plan not found");
        }

        [Fact]
        public async Task Handle_ApprovedPlan_ThrowsException()
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

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = planId,
                ReviewNote = "Reject"
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Approved plan cannot be rejected");
        }

        [Fact]
        public async Task Handle_ValidRequest_RejectsPlan()
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

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = planId,
                ReviewNote = "Budget too high"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            plan.Status.Should().Be("Rejected");
            plan.ReviewNote.Should().Be("Budget too high");
            plan.ApprovedById.Should().Be(userId);

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}