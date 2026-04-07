using ERMS.Application.Features.Training.Commands.DeleteTrainingRequest;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.DeleteTrainingRequest
{
    public class DeleteTrainingRequestHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();

        private readonly DeleteTrainingRequestHandler _handler;

        public DeleteTrainingRequestHandlerTests()
        {
            _handler = new DeleteTrainingRequestHandler(_contextMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenNotFound()
        {
            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(new List<TrainingRequest>()
                .AsQueryable().BuildMockDbSet().Object);

            var command = new DeleteTrainingRequestCommand
            {
                Id = Guid.NewGuid()
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldSoftDeleteSuccessfully()
        {
            var id = Guid.NewGuid();

            var entity = new TrainingRequest
            {
                Id = id,
                IsDeleted = false
            };

            var data = new List<TrainingRequest> { entity };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new DeleteTrainingRequestCommand
            {
                Id = id
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            entity.IsDeleted.Should().BeTrue();
            entity.DeletedAt.Should().NotBeNull();
        }
    }
}