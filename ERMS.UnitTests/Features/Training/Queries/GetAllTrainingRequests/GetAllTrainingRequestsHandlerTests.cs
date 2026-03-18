using ERMS.Application.Features.Training.Queries.GetAllTrainingRequests;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Queries.GetAllTrainingRequests
{
    public class GetAllTrainingRequestsHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetAllTrainingRequestsHandler _handler;

        public GetAllTrainingRequestsHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetAllTrainingRequestsHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object);
        }

        private void SetupMockContext(List<TrainingRequest> requests)
        {
            var dbSetMock = requests.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(c => c.TrainingRequests).Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsTrainingRequests()
        {
            // Arrange
            var department = new Department { DepartmentName = "IT" };
            var user = new User { FullName = "John Doe" };

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Docker Training",
                    Status = "Pending",
                    Urgency = "High",
                    Department = department,
                    RequestedBy = user,
                    TargetAudience = "Developers",
                    EstimatedParticipants = 10,
                    EstimatedBudget = 500,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Kubernetes Training",
                    Status = "Approved",
                    Urgency = "Medium",
                    Department = department,
                    RequestedBy = user,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var query = new GetAllTrainingRequestsQuery
            {
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithSearchFilter_ReturnsFilteredResults()
        {
            // Arrange
            var department = new Department { DepartmentName = "HR" };
            var user = new User { FullName = "Alice Smith" };

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Leadership Training",
                    RequestedBy = user,
                    Department = department,
                    Status = "Pending",
                    Urgency = "High",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Technical Training",
                    RequestedBy = user,
                    Department = department,
                    Status = "Pending",
                    Urgency = "Low",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var query = new GetAllTrainingRequestsQuery
            {
                Search = "leader"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().Subject.Should().Contain("Leadership");
        }

        [Fact]
        public async Task Handle_WithDepartmentFilter_ReturnsFilteredResults()
        {
            // Arrange
            var dep1 = new Department { Id = 1, DepartmentName = "IT" };
            var dep2 = new Department { Id = 2, DepartmentName = "HR" };

            var user = new User { FullName = "User" };

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "DevOps",
                    Department = dep1,
                    DepartmentId = dep1.Id,
                    RequestedBy = user,
                    Status = "Pending",
                    Urgency = "High",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Soft Skills",
                    Department = dep2,
                    DepartmentId = dep2.Id,
                    RequestedBy = user,
                    Status = "Pending",
                    Urgency = "Low",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var query = new GetAllTrainingRequestsQuery
            {
                DepartmentId = 1
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().DepartmentName.Should().Be("IT");
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            var department = new Department { DepartmentName = "Finance" };
            var user = new User { FullName = "User" };

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Excel Training",
                    Status = "Approved",
                    Urgency = "Medium",
                    Department = department,
                    RequestedBy = user,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Accounting Training",
                    Status = "Pending",
                    Urgency = "Medium",
                    Department = department,
                    RequestedBy = user,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var query = new GetAllTrainingRequestsQuery
            {
                Status = "Approved"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().Status.Should().Be("Approved");
        }

        [Fact]
        public async Task Handle_ShouldSortByCreatedAtDescending()
        {
            // Arrange
            var department = new Department { DepartmentName = "IT" };
            var user = new User { FullName = "User" };

            var requests = new List<TrainingRequest>
            {
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "Old Request",
                    Department = department,
                    RequestedBy = user,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Status = "Pending",
                    Urgency = "Low",
                    IsDeleted = false
                },
                new TrainingRequest
                {
                    Id = Guid.NewGuid(),
                    Subject = "New Request",
                    Department = department,
                    RequestedBy = user,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    Urgency = "High",
                    IsDeleted = false
                }
            };

            SetupMockContext(requests);

            var query = new GetAllTrainingRequestsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.First().Subject.Should().Be("New Request");
        }
    }
}