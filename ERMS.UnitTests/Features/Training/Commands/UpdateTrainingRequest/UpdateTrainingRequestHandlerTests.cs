using ERMS.Application.Features.Training.Commands.UpdateTrainingRequest;
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

namespace ERMS.UnitTests.Features.Training.Commands.UpdateTrainingRequest
{
    public class UpdateTrainingRequestHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<UpdateTrainingRequestHandler>> _loggerMock = new();

        private readonly UpdateTrainingRequestHandler _handler;

        public UpdateTrainingRequestHandlerTests()
        {
            _handler = new UpdateTrainingRequestHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
        {
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new UpdateTrainingRequestCommand
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
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(new List<TrainingRequest>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid()
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy yêu cầu đào tạo");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenUserNotOwner()
        {
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var trainingRequests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    RequestedById = Guid.NewGuid(), // khác userId
                    Status = "NeedRevision",
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(trainingRequests.AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = trainingRequests[0].Id
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Bạn không có quyền cập nhật yêu cầu này");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenStatusNotNeedRevision()
        {
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var trainingRequests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    RequestedById = userId,
                    Status = "Approved",
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(trainingRequests.AsQueryable().BuildMockDbSet().Object);

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = trainingRequests[0].Id
            };

            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Only requests requiring revision can be updated");
        }

        [Fact]
        public async Task Handle_ShouldUpdateTrainingRequestSuccessfully()
        {
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var trainingRequest = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                RequestedById = userId,
                Subject = "Old subject",
                Status = "NeedRevision",
                IsDeleted = false
            };

            var list = new List<TrainingRequest> { trainingRequest };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(list.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = trainingRequest.Id,
                Subject = "New subject",
                EstimatedParticipants = 20
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();

            trainingRequest.Subject.Should().Be("New subject");
            trainingRequest.Status.Should().Be("Pending");
            trainingRequest.ReviewNote.Should().BeNull();
        }
    }
}