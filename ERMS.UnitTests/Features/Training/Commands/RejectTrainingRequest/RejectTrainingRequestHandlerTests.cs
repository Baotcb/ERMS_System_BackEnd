using ERMS.Application.Features.Training.Commands.ConfirmTrainingRequest;
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

namespace ERMS.UnitTests.Features.Training.Commands.RejectTrainingRequest
{
    public class RejectTrainingRequestHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<RejectTrainingRequestHandler>> _loggerMock;
        private readonly RejectTrainingRequestHandler _handler;

        public RejectTrainingRequestHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<RejectTrainingRequestHandler>>();

            _handler = new RejectTrainingRequestHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private void SetupMockContext(List<TrainingRequest> requests)
        {
            var dbSetMock = requests.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.TrainingRequests).Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_UserNotLoggedIn_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid(),
                ReviewNote = "Not needed"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_TrainingRequestNotFound_ThrowsException()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupMockContext(new List<TrainingRequest>());

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = Guid.NewGuid(),
                ReviewNote = "Reject"
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Training request not found");
        }

        [Fact]
        public async Task Handle_RequestAlreadyConfirmed_ThrowsException()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = requestId,
                    EnterpriseId = enterpriseId,
                    Status = "AddedToPlan",
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = requestId,
                ReviewNote = "Reject"
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Request already confirmed");
        }

        [Fact]
        public async Task Handle_RequestAlreadyRejected_ThrowsException()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = requestId,
                    EnterpriseId = enterpriseId,
                    Status = "Rejected",
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = requestId,
                ReviewNote = "Reject"
            };

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Request already rejected");
        }

        [Fact]
        public async Task Handle_ValidRequest_RejectsTrainingRequest()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var requestId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var request = new TrainingRequest
            {
                Id = requestId,
                EnterpriseId = enterpriseId,
                Status = "Pending",
                IsDeleted = false
            };

            SetupMockContext(new List<TrainingRequest> { request });

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new RejectTrainingRequestCommand
            {
                TrainingRequestId = requestId,
                ReviewNote = "Budget not approved"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            request.Status.Should().Be(TrainingRequestStatus.Rejected);
            request.ReviewNote.Should().Be("Budget not approved");

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}