using ERMS.Application.Features.Courses.Queries.GetCourseDetails;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Courses.Queries.GetCourseDetails
{
    public class GetCourseDetailsHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        private readonly GetCourseDetailsHandler _handler;

        public GetCourseDetailsHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetCourseDetailsHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object);
        }

        private void SetupCourses(List<Course> courses)
        {
            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task Handle_UserWithoutEnterprise_ShouldThrowException()
        {
            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var query = new GetCourseDetailsQuery
            {
                Id = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User does not belong to any enterprise");
        }

        [Fact]
        public async Task Handle_CourseNotFound_ShouldThrowException()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupCourses(new List<Course>());

            var query = new GetCourseDetailsQuery
            {
                Id = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Course not found");
        }

        [Fact]
        public async Task Handle_ReturnCourseDetails()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupCourses(new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId,
                    CourseName = "ASP.NET Core",
                    CourseCode = "ASP01",
                    CompletionCriteria = "Finish all lessons",
                    CreatedAt = DateTime.UtcNow,
                     Trainer = new Employee
            {
                User = new User
                {
                    FullName = "Trainer Test"
                }
            },

                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>(),
                    CourseSkills = new List<CourseSkill>()
                }
            });

            var query = new GetCourseDetailsQuery
            {
                Id = courseId
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().Be(courseId);
            result.CourseName.Should().Be("ASP.NET Core");
        }
    }
}