using ERMS.Application.Features.Courses.Commands.PublishCourse;
using ERMS.Application.Interface;
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

namespace ERMS.UnitTests.Features.Courses.Commands
{
    public class PublishCourseCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<ILogger<PublishCourseCommandHandler>> _loggerMock = new();

        private readonly PublishCourseCommandHandler _handler;

        public PublishCourseCommandHandlerTests()
        {
            _handler = new PublishCourseCommandHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_PublishCourseSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);

            var courses = new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    EnterpriseId = enterpriseId,
                    Status = "Draft"
                }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            var lessons = new List<Lesson>
            {
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId
                }
            };

            _contextMock.Setup(x => x.Lessons)
                .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

            var command = new PublishCourseCommand
            {
                Id = courseId
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            courses[0].Status.Should().Be("Published");
        }
    }
}