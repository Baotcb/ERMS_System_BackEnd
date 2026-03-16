using ERMS.Application.Features.Training.Commands.RejectTrainingPlan;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.Training.Commands.RejectTrainingPlan
{
    public class RejectTrainingPlanHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<RejectTrainingPlanHandler>> _loggerMock = new();

        private readonly RejectTrainingPlanHandler _handler;

        public RejectTrainingPlanHandlerTests()
        {
            _handler = new RejectTrainingPlanHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenTrainingPlanNotFound()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(new List<TrainingPlan>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch đào tạo");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenPlanAlreadyApproved()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var plans = new List<TrainingPlan>
            {
                new TrainingPlan
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    Status = "Approved",
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(plans.AsQueryable().BuildMockDbSet().Object);

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = plans[0].Id
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Kế hoạch đã được phê duyệt, không thể từ chối");
        }

        [Fact]
        public async Task Handle_ShouldRejectTrainingPlanSuccessfully()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var plan = new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Status = "Pending",
                IsDeleted = false
            };

            var list = new List<TrainingPlan> { plan };

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(list.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new RejectTrainingPlanCommand
            {
                TrainingPlanId = plan.Id,
                ReviewNote = "Ngân sách chưa phù hợp"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            plan.Status.Should().Be("Rejected");
            plan.ReviewNote.Should().Be("Ngân sách chưa phù hợp");
            plan.ApprovedById.Should().Be(userId);
            plan.ApprovedAt.Should().NotBeNull();
        }
    }
}