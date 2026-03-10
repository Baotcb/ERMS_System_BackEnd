using ERMS.Application.Features.Training.Commands.UpdateTrainingRequest;
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

namespace ERMS.UnitTests.Features.Training.Commands.UpdateTrainingRequest
{
    public class UpdateTrainingRequestHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<UpdateTrainingRequestHandler>> _loggerMock;
        private readonly UpdateTrainingRequestHandler _handler;

        public UpdateTrainingRequestHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<UpdateTrainingRequestHandler>>();

            _handler = new UpdateTrainingRequestHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private void SetupMockContext(List<TrainingRequest> requests)
        {
            var dbSetMock = requests.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_UserNotLoggedIn_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns((Guid?)null);

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid()
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RequestNotFound_ThrowsException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());

            SetupMockContext(new List<TrainingRequest>());

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid()
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Training request not found");
        }

        [Fact]
        public async Task Handle_UserNotOwner_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            var request = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                RequestedById = Guid.NewGuid(), // khác user
                Status = "NeedRevision",
                IsDeleted = false
            };

            SetupMockContext(new List<TrainingRequest> { request });

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = request.Id
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("You are not allowed to update this request");
        }

        [Fact]
        public async Task Handle_StatusNotNeedRevision_ThrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            var request = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                RequestedById = userId,
                Status = "Pending",
                IsDeleted = false
            };

            SetupMockContext(new List<TrainingRequest> { request });

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = request.Id
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Only requests requiring revision can be updated");
        }

        [Fact]
        public async Task Handle_ValidUpdate_UpdatesTrainingRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId)
                .Returns(userId);

            var request = new TrainingRequest
            {
                Id = Guid.NewGuid(),
                RequestedById = userId,
                Subject = "Old subject",
                Urgency = "Low",
                Description = "Old desc",
                TargetAudience = "Old audience",
                EstimatedParticipants = 5,
                EstimatedBudget = 100,
                Status = "NeedRevision",
                IsDeleted = false
            };

            SetupMockContext(new List<TrainingRequest> { request });

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new UpdateTrainingRequestCommand
            {
                TrainingRequestId = request.Id,
                Subject = "New subject",
                Urgency = "High",
                Description = "New desc",
                TargetAudience = "Developers",
                EstimatedParticipants = 20,
                EstimatedBudget = 2000
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            request.Subject.Should().Be("New subject");
            request.Urgency.Should().Be("High");
            request.Description.Should().Be("New desc");
            request.TargetAudience.Should().Be("Developers");
            request.EstimatedParticipants.Should().Be(20);
            request.EstimatedBudget.Should().Be(2000);

            request.Status.Should().Be("Pending");
            request.ReviewNote.Should().BeNull();

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}