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
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
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
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee
                    {
                        User = new User
                        {
                            FullName = "Trainer Test"
                        }
                    },
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

        [Fact]
        public async Task Handle_SearchCourse_ShouldReturnFilteredResult()
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
                    CourseName = "ASP.NET Core",
                    CourseCode = "ASP01",
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    CourseName = "Java Basics",
                    CourseCode = "JAVA01",
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                }
            });

            var query = new GetAllCourseQuery
            {
                Search = "asp",
                Page = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Items.Should().HaveCount(1);
            result.Items[0].CourseName.Should().Be("ASP.NET Core");
        }

        [Fact]
        public async Task Handle_FilterByStatus_ShouldReturnCorrectCourses()
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
                    CourseName = "Draft Course",
                    CourseCode = "DRAFT01",
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    CourseName = "Published Course",
                    CourseCode = "PUB01",
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                }
            });

            var query = new GetAllCourseQuery
            {
                Status = "Published",
                Page = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Items.Should().HaveCount(1);
            result.Items[0].Status.Should().Be("Published");
        }

        [Fact]
        public async Task Handle_FilterByMandatory_ShouldReturnCorrectCourses()
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
                    CourseName = "Mandatory Course",
                    CourseCode = "MAN01",
                    IsMandatory = true,
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                },
                new Course
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    CourseName = "Optional Course",
                    CourseCode = "OPT01",
                    IsMandatory = false,
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                }
            });

            var query = new GetAllCourseQuery
            {
                IsMandatory = true,
                Page = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Items.Should().HaveCount(1);
            result.Items[0].IsMandatory.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Pagination_ShouldReturnCorrectPage()
        {
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>();

            for (int i = 1; i <= 15; i++)
            {
                courses.Add(new Course
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    CourseName = $"Course {i}",
                    CourseCode = $"C{i}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>(),
                    Enrollments = new List<Enrollment>()
                });
            }

            SetupCourses(courses);

            var query = new GetAllCourseQuery
            {
                Page = 2,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(15);
        }

        [Fact]
        public async Task Handle_ShouldCalculateLessonAndEnrollmentCount()
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
                    CourseName = "Test Course",
                    CourseCode = "TEST01",
                    CreatedAt = DateTime.UtcNow,
                    Trainer = new Employee { User = new User { FullName = "Trainer" } },
                    Lessons = new List<Lesson>
                    {
                        new Lesson { IsDeleted = false },
                        new Lesson { IsDeleted = false }
                    },
                    Enrollments = new List<Enrollment>
                    {
                        new Enrollment { IsDeleted = false }
                    }
                }
            });

            var query = new GetAllCourseQuery
            {
                Page = 1,
                PageSize = 10
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Items[0].LessonCount.Should().Be(2);
            result.Items[0].EnrollmentCount.Should().Be(1);
        }
    }
}