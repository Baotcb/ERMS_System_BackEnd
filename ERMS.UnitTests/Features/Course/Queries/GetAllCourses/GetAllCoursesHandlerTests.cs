using ERMS.Application.Features.Courses.Queries.GetAllCourses;
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

namespace ERMS.UnitTests.Features.Courses.Queries.GetAllCourses
{
    public class GetAllCoursesHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        private readonly GetAllCoursesHandler _handler;

        public GetAllCoursesHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetAllCoursesHandler(
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

            var query = new GetAllCourseQuery();

            Func<Task> act = () => _handler.Handle(query, CancellationToken.None);

            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User does not belong to any enterprise");
        }

        [Fact]
        public async Task Handle_ReturnCoursesSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            SetupCourses(new List<Course>
            {
                new Course
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    CourseName = "C# Basics",
                    CourseCode = "CSHARP01",
                    Status = "Published",
                     Trainer = new Employee
            {
                User = new User
                {
                    FullName = "Trainer Test"
                }
            },

                    CreatedAt = DateTime.UtcNow,
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                }
            });

            var query = new GetAllCourseQuery
            {
                Page = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
        }
    }
}