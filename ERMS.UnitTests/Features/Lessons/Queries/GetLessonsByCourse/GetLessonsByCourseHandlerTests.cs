using ERMS.Application.Features.Lessons.Queries.GetLessonsByCourse;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Lessons.Queries.GetLessonsByCourse
{
    public class GetLessonsByCourseHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly GetLessonsByCourseHandler _handler;

        public GetLessonsByCourseHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();

            _handler = new GetLessonsByCourseHandler(_contextMock.Object);
        }

        private void SetupLessons(List<Lesson> lessons)
        {
            var dbSetMock = lessons.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.Lessons)
                .Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_NoLessons_ReturnsEmptyList()
        {
            // Arrange
            SetupLessons(new List<Lesson>());

            var query = new GetLessonsByCourseQuery
            {
                CourseId = Guid.NewGuid()
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ReturnsLessonsOfCourse()
        {
            // Arrange
            var courseId = Guid.NewGuid();

            SetupLessons(new List<Lesson>
            {
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    LessonTitle = "Lesson 1",
                    OrderIndex = 1,
                    ContentType = "Video",
                    IsDeleted = false
                },
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    LessonTitle = "Lesson 2",
                    OrderIndex = 2,
                    ContentType = "Document",
                    IsDeleted = false
                }
            });

            var query = new GetLessonsByCourseQuery
            {
                CourseId = courseId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Select(x => x.LessonTitle)
                .Should().Contain(new[] { "Lesson 1", "Lesson 2" });
        }

        [Fact]
        public async Task Handle_IgnoreDeletedLessons()
        {
            // Arrange
            var courseId = Guid.NewGuid();

            SetupLessons(new List<Lesson>
            {
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    LessonTitle = "Active Lesson",
                    OrderIndex = 1,
                    ContentType = "Video",
                    IsDeleted = false
                },
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    LessonTitle = "Deleted Lesson",
                    OrderIndex = 2,
                    ContentType = "Video",
                    IsDeleted = true
                }
            });

            var query = new GetLessonsByCourseQuery
            {
                CourseId = courseId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result.First().LessonTitle.Should().Be("Active Lesson");
        }

        [Fact]
        public async Task Handle_ShouldReturnLessonsOrderedByOrderIndex()
        {
            // Arrange
            var courseId = Guid.NewGuid();

            SetupLessons(new List<Lesson>
            {
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    LessonTitle = "Lesson 2",
                    OrderIndex = 2,
                    ContentType = "Video",
                    IsDeleted = false
                },
                new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    LessonTitle = "Lesson 1",
                    OrderIndex = 1,
                    ContentType = "Video",
                    IsDeleted = false
                }
            });

            var query = new GetLessonsByCourseQuery
            {
                CourseId = courseId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result[0].LessonTitle.Should().Be("Lesson 1");
            result[1].LessonTitle.Should().Be("Lesson 2");
        }
    }
}