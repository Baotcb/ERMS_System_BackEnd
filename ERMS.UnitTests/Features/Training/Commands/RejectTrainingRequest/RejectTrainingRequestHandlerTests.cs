using ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest;
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

namespace ERMS.UnitTests.Features.Training.Commands.ConfirmTrainingRequest
{
    public class RejectTrainingRequestHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<RejectTrainingRequestHandler>> _loggerMock = new();

        private readonly RejectTrainingRequestHandler _handler;

        public RejectTrainingRequestHandlerTests()
        {
            _handler = new RejectTrainingRequestHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenTrainingRequestNotFound()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(new List<TrainingRequest>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy yêu cầu đào tạo");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenRequestAlreadyApproved()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    Status = "AddedToPlan",
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(requests.AsQueryable().BuildMockDbSet().Object);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = requests[0].Id
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Yêu cầu đã được phê duyệt");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenRequestAlreadyRejected()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    Status = "Rejected",
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(requests.AsQueryable().BuildMockDbSet().Object);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = requests[0].Id
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Yêu cầu đã bị từ chối");
        }

        [Fact]
        public async Task Handle_ShouldRejectTrainingRequestSuccessfully()
        {
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var trainingRequest = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Status = "Pending",
                IsDeleted = false
            };

            var list = new List<TrainingRequest> { trainingRequest };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(list.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = trainingRequest.Id,
                ReviewNote = "Không phù hợp ngân sách"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            trainingRequest.Status.Should().Be("Rejected");
            trainingRequest.ReviewNote.Should().Be("Không phù hợp ngân sách");
        }
    }
}