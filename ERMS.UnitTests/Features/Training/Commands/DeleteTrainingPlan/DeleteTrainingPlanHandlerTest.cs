using ERMS.Application.Features.Training.Commands.DeleteTrainingPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.DeleteTrainingPlan
{
    public class DeleteTrainingPlanHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<DeleteTrainingPlanHandler>> _loggerMock;
        private readonly DeleteTrainingPlanHandler _handler;

        public DeleteTrainingPlanHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<DeleteTrainingPlanHandler>>();

            _handler = new DeleteTrainingPlanHandler(
                _context,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldDeleteTrainingPlanSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var plan = new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CreatedById = userId,
                Status = "Draft",
                PlanCode = "TP001",
                PlanName = "Kế hoạch đào tạo 1",

            };

            _context.TrainingPlans.Add(plan);
            await _context.SaveChangesAsync();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var result = await _handler.Handle(new DeleteTrainingPlanCommand { Id = plan.Id }, CancellationToken.None);

            result.Should().Be(plan.Id);
            plan.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenApproved()
        {
            var enterpriseId = Guid.NewGuid();

            var plan = new TrainingPlan
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Status = "Approved",
                    PlanCode = "TP002",
                    PlanName = "Kế hoạch đào tạo 2",
            };

            _context.TrainingPlans.Add(plan);
            await _context.SaveChangesAsync();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new[] { AppRoles.HRManager});
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var act = () => _handler.Handle(new DeleteTrainingPlanCommand { Id = plan.Id }, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không thể xóa kế hoạch đã được phê duyệt");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}