using ERMS.Application.Features.Training.Queries.GetAllTrainingPlans;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
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

namespace ERMS.UnitTests.Features.Training.Queries.GetAllTrainingPlans
{
    public class GetAllTrainingPlansHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<GetAllTrainingPlansHandler>> _loggerMock;
        private readonly GetAllTrainingPlansHandler _handler;

        public GetAllTrainingPlansHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<GetAllTrainingPlansHandler>>();

            _handler = new GetAllTrainingPlansHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private void SetupMockContext(List<TrainingPlan> plans)
        {
            var dbSetMock = plans.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_EnterpriseIdNull_ThrowsUnauthorizedAccessException()
        {
            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var query = new GetAllTrainingPlansQuery();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(query, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ReturnsPagedResultCorrectly()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var user = new User { FullName = "Admin" };

            var plans = new List<TrainingPlan>
        {
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Plan A",
                PlanCode = "A",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            },
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Plan B",
                PlanCode = "B",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            }
        };

            SetupMockContext(plans);

            var query = new GetAllTrainingPlansQuery
            {
                Page = 1,
                PageSize = 1
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(1);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(1);
            result.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task Handle_SearchFilter_Works()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var user = new User { FullName = "Admin" };

            var plans = new List<TrainingPlan>
        {
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Leadership Plan",
                PlanCode = "LP",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            },
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Technical Plan",
                PlanCode = "TP",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            }
        };

            SetupMockContext(plans);

            var query = new GetAllTrainingPlansQuery
            {
                Search = "Leader"
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].PlanName.Should().Contain("Leadership");
        }

        [Fact]
        public async Task Handle_StatusFilter_Works()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var user = new User { FullName = "Admin" };

            var plans = new List<TrainingPlan>
        {
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Plan 1",
                PlanCode = "P1",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            },
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Plan 2",
                PlanCode = "P2",
                Status = "Approved",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            }
        };

            SetupMockContext(plans);

            var query = new GetAllTrainingPlansQuery
            {
                Status = "Approved"
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.TotalCount.Should().Be(1);
            result.Items[0].Status.Should().Be("Approved");
        }

        [Fact]
        public async Task Handle_SortByCreatedAtDescending_Works()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var user = new User { FullName = "Admin" };

            var plans = new List<TrainingPlan>
        {
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "Old Plan",
                PlanCode = "OLD",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            },
            new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                PlanName = "New Plan",
                PlanCode = "NEW",
                Status = "Draft",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
                Courses = new List<Course>(),
                IsDeleted = false
            }
        };

            SetupMockContext(plans);

            var result = await _handler.Handle(
                new GetAllTrainingPlansQuery(),
                CancellationToken.None);

            result.Items[0].PlanName.Should().Be("New Plan");
        }
    }
}