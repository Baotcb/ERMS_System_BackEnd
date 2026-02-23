using ERMS.Application.Features.PlanDetails.Queries.GetAllPlanDetails;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Identity;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.PlanDetails.Queries.GetAllPlanDetails
{
    public class GetAllPlanDetailsHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetAllPlanDetailsHandler _handler;

        public GetAllPlanDetailsHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetAllPlanDetailsHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoDetailsExist()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
            var query = new GetAllPlanDetailsQuery { RecruitmentPlanId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldReturnPlanDetails_WhenSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var user = new User { Id = userId, FullName = "Requester" };
            _context.Users.Add(user);

            var plan = new RecruitmentPlan { Id = planId, EnterpriseId = enterpriseId, PlanName = "P1", CreatedById = userId, PlanCode = "P1" };
            _context.RecruitmentPlans.Add(plan);

            _context.PlanDetails.AddRange(new List<PlanDetail>
            {
                new PlanDetail { Id = Guid.NewGuid(), RecruitmentPlanId = planId, PositionTitle = "Pos 1", RequestedById = userId, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new PlanDetail { Id = Guid.NewGuid(), RecruitmentPlanId = planId, PositionTitle = "Pos 2", RequestedById = userId, IsDeleted = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new PlanDetail { Id = Guid.NewGuid(), RecruitmentPlanId = planId, PositionTitle = "Deleted", RequestedById = userId, IsDeleted = true },
                new PlanDetail { Id = Guid.NewGuid(), RecruitmentPlanId = Guid.NewGuid(), PositionTitle = "Other Plan", RequestedById = userId }
            });
            await _context.SaveChangesAsync();

            var query = new GetAllPlanDetailsQuery { RecruitmentPlanId = planId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.First().PositionTitle.Should().Be("Pos 2"); // Ordered by CreatedAt ASC
            result.Last().PositionTitle.Should().Be("Pos 1");
            result.All(d => d.RequestedByName == "Requester").Should().BeTrue();
        }
    }
}
