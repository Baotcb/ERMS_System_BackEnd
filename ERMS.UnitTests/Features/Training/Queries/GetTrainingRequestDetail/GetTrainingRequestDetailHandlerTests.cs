using ERMS.Application.Features.Training.Queries.GetTrainingRequestDetail;
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

namespace ERMS.UnitTests.Features.Training.Queries.GetTrainingRequestDetail
{
    public class GetTrainingRequestDetailHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();

        private readonly GetTrainingRequestDetailHandler _handler;

        public GetTrainingRequestDetailHandlerTests()
        {
            _handler = new GetTrainingRequestDetailHandler(_contextMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenNotFound()
        {
            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(new List<TrainingRequest>()
                .AsQueryable().BuildMockDbSet().Object);

            var query = new GetTrainingRequestDetailQuery
            {
                Id = Guid.NewGuid()
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldReturnTrainingRequestDetail()
        {
            var id = Guid.NewGuid();

            var data = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = id,
                    EnterpriseId = Guid.NewGuid(),
                    Subject = "Test",
                    Status = "Pending",
                    Urgency = "High",
                    Department = new Domain.Entities.Organization.Department
                    {
                        DepartmentName = "IT"
                    },
                    RequestedBy = new Domain.Entities.Identity.User
                    {
                        FullName = "John Doe"
                    },
                    IsDeleted = false
                }
            };

            _contextMock.Setup(x => x.TrainingRequests)
                .Returns(data.AsQueryable().BuildMockDbSet().Object);

            var query = new GetTrainingRequestDetailQuery
            {
                Id = id
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.Subject.Should().Be("Test");
            result.DepartmentName.Should().Be("IT");
            result.RequestedByName.Should().Be("John Doe");
        }
    }
}