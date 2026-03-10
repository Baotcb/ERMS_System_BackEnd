using ERMS.Application.Features.Lessons.Commands.CreateLesson;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Lessons.Commands.CreateLesson
{
    public class CreateLessonHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly CreateLessonHandler _handler;

        public CreateLessonHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new CreateLessonHandler(_contextMock.Object);
        }

        private void SetupCourses(List<Course> courses)
        {
            var dbSet = courses.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Courses)
                .Returns(dbSet.Object);
        }

        [Fact]
        public async Task Handle_CourseNotFound_ShouldThrowException()
        {
            SetupCourses(new List<Course>());

            var command = new CreateLessonCommand
            {
                CourseId = Guid.NewGuid(),
                LessonTitle = "Lesson 1"
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Course not found");
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateLesson()
        {
            var courseId = Guid.NewGuid();

            SetupCourses(new List<Course>
            {
                new Course
                {
                    Id = courseId,
                    IsDeleted = false
                }
            });

            _contextMock.Setup(x => x.Lessons.Add(It.IsAny<Lesson>()));

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateLessonCommand
            {
                CourseId = courseId,
                LessonTitle = "New Lesson",
                OrderIndex = 1
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();

            _contextMock.Verify(x =>
                x.Lessons.Add(It.IsAny<Lesson>()),
                Times.Once);

            _contextMock.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}