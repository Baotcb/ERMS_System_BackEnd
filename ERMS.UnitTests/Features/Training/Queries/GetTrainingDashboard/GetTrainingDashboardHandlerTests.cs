
using ERMS.Application.Features.Dashboard.Queries.GetTrainingDashboard;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
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

namespace ERMS.UnitTests.Features.Dashboard.Queries.GetTrainingDashboard
{
    public class GetTrainingDashboardHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<GetTrainingDashboardHandler>> _loggerMock;
        private readonly GetTrainingDashboardHandler _handler;

        public GetTrainingDashboardHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<GetTrainingDashboardHandler>>();

            _handler = new GetTrainingDashboardHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private void SetupMockContext(
            List<TrainingPlan> plans,
            List<Course> courses,
            List<Enrollment> enrollments)
        {
            _contextMock.Setup(x => x.TrainingPlans)
                .Returns(plans.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task Handle_UserNotDirector_ThrowsUnauthorized()
        {
            _currentUserServiceMock
                .Setup(x => x.Roles)
                .Returns(new List<string> { AppRoles.HRManager });

            var act = async () => await _handler.Handle(
                new GetTrainingDashboardQuery(),
                CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Chỉ Director mới được xem dashboard");
        }

        [Fact]
        public async Task Handle_EnterpriseIdNull_ThrowsException()
        {
            _currentUserServiceMock
                .Setup(x => x.Roles)
                .Returns(new List<string> { AppRoles.Director });

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var act = async () => await _handler.Handle(
                new GetTrainingDashboardQuery(),
                CancellationToken.None);

            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task Handle_ReturnsCorrectDashboardData()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.Roles)
                .Returns(new List<string> { AppRoles.Director });

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var plans = new List<TrainingPlan>
            {
                new TrainingPlan
                {
                    EnterpriseId = enterpriseId,
                    Status = "Approved",
                    TotalBudget = 1000,
                    IsDeleted = false
                },
                new TrainingPlan
                {
                    EnterpriseId = enterpriseId,
                    Status = "Pending",
                    IsDeleted = false
                }
            };

            var courses = new List<Course>
            {
                new Course
                {
                    EnterpriseId = enterpriseId,
                    Status = "Published",
                    IsDeleted = false,
                    Lessons = new List<Lesson>
                    {
                        new Lesson { IsDeleted = false }
                    }
                },
                new Course
                {
                    EnterpriseId = enterpriseId,
                    Status = "Draft",
                    IsDeleted = false,
                    Lessons = new List<Lesson>()
                }
            };

            var enrollments = new List<Enrollment>
            {
                new Enrollment { IsDeleted = false },
                new Enrollment { IsDeleted = false }
            };

            SetupMockContext(plans, courses, enrollments);

            var result = await _handler.Handle(
                new GetTrainingDashboardQuery(),
                CancellationToken.None);

            var data = result.Data;

            data.TotalPlans.Should().Be(2);
            data.PendingPlans.Should().Be(1);
            data.ApprovedPlans.Should().Be(1);
            data.ApprovedBudget.Should().Be(1000);

            data.TotalCourses.Should().Be(2);
            data.ActiveCourses.Should().Be(1);
            data.CoursesWithContent.Should().Be(1);

            data.TotalEnrollments.Should().Be(2);
            data.AvgStudentsPerCourse.Should().Be(1);
        }

        [Fact]
        public async Task Handle_FiltersDeleted_AndOtherEnterprise()
        {
            var myEnterprise = Guid.NewGuid();
            var otherEnterprise = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.Roles)
                .Returns(new List<string> { AppRoles.Director });

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(myEnterprise);

            var plans = new List<TrainingPlan>
            {
                new TrainingPlan { EnterpriseId = myEnterprise, Status = "Approved", TotalBudget = 1000, IsDeleted = false },
                new TrainingPlan { EnterpriseId = myEnterprise, IsDeleted = true },
                new TrainingPlan { EnterpriseId = otherEnterprise, Status = "Approved", TotalBudget = 9999 }
            };

            var courses = new List<Course>
            {
                new Course { EnterpriseId = myEnterprise, Status = "Published", IsDeleted = false },
                new Course { EnterpriseId = otherEnterprise, Status = "Published" }
            };

            var enrollments = new List<Enrollment>
            {
                new Enrollment { IsDeleted = false },
                new Enrollment { IsDeleted = true }
            };

            SetupMockContext(plans, courses, enrollments);

            var result = await _handler.Handle(
                new GetTrainingDashboardQuery(),
                CancellationToken.None);

            var data = result.Data;

            data.TotalPlans.Should().Be(1);
            data.ApprovedPlans.Should().Be(1);
            data.ApprovedBudget.Should().Be(1000);

            data.TotalCourses.Should().Be(1);
            data.ActiveCourses.Should().Be(1);

            data.TotalEnrollments.Should().Be(1);
        }

        [Fact]
        public async Task Handle_NoCourses_ShouldReturnZeroAverage()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.Roles)
                .Returns(new List<string> { AppRoles.Director });

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupMockContext(
                new List<TrainingPlan>(),
                new List<Course>(),
                new List<Enrollment> { new Enrollment() }
            );

            var result = await _handler.Handle(
                new GetTrainingDashboardQuery(),
                CancellationToken.None);

            result.Data.AvgStudentsPerCourse.Should().Be(0);
        }
    }
}
