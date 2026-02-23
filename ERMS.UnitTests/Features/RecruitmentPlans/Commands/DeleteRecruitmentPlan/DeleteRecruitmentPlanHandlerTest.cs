using ERMS.Application.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.RecruitmentPlans.Commands.DeleteRecruitmentPlan
{
    public class DeleteRecruitmentPlanHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<DeleteRecruitmentPlanHandler>> _loggerMock;
        private readonly DeleteRecruitmentPlanHandler _handler;

        public DeleteRecruitmentPlanHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<DeleteRecruitmentPlanHandler>>();

            _handler = new DeleteRecruitmentPlanHandler(_context, _currentUserServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenPlanNotFound()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(Guid.NewGuid());
            var command = new DeleteRecruitmentPlanCommand { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy kế hoạch tuyển dụng");
        }

        [Fact]
        public async Task Handle_ShouldSoftDeletePlan_WhenSuccessful()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var planId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var plan = new RecruitmentPlan 
            { 
                Id = planId, 
                EnterpriseId = enterpriseId, 
                PlanCode = "P1", 
                PlanName = "Plan 1", 
                CreatedById = Guid.NewGuid(),
                IsDeleted = false
            };
            _context.RecruitmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            var command = new DeleteRecruitmentPlanCommand { Id = planId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedPlan = await _context.RecruitmentPlans.FindAsync(planId);
            updatedPlan!.IsDeleted.Should().BeTrue();
            updatedPlan.DeletedAt.Should().NotBeNull();
        }
    }
}
