using ERMS.Application.Features.Lessons.Queries.GetLessonProgress;
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

namespace ERMS.UnitTests.Features.Lessons.Queries.GetLessonProgress
{
    public class GetLessonProgressByEnrollmentHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly GetLessonProgressByEnrollmentHandler _handler;

        public GetLessonProgressByEnrollmentHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();

            _handler = new GetLessonProgressByEnrollmentHandler(_contextMock.Object);
        }

        private void SetupData(
            List<Lesson> lessons,
            List<Enrollment> enrollments,
            List<LessonProgress> progresses)
        {
            _contextMock.Setup(x => x.Lessons)
                .Returns(lessons.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Enrollments)
                .Returns(enrollments.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.LessonProgresses)
                .Returns(progresses.AsQueryable().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task Handle_NoLessons_ReturnsEmptyList()
        {
            // Arrange
            SetupData(
                new List<Lesson>(),
                new List<Enrollment>(),
                new List<LessonProgress>());

            var query = new GetLessonProgressByEnrollmentQuery
            {
                EnrollmentId = Guid.NewGuid()
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ReturnLessonsForEnrollment()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();

            SetupData(
                new List<Lesson>
                {
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = courseId,
                        LessonTitle = "Lesson 1",
                        OrderIndex = 1,
                        IsDeleted = false
                    }
                },
                new List<Enrollment>
                {
                    new Enrollment
                    {
                        Id = enrollmentId,
                        CourseId = courseId
                    }
                },
                new List<LessonProgress>()
            );

            var query = new GetLessonProgressByEnrollmentQuery
            {
                EnrollmentId = enrollmentId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].LessonTitle.Should().Be("Lesson 1");
        }

        [Fact]
        public async Task Handle_ReturnProgressInformation()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();

            SetupData(
                new List<Lesson>
                {
                    new Lesson
                    {
                        Id = lessonId,
                        CourseId = courseId,
                        LessonTitle = "Lesson 1",
                        OrderIndex = 1,
                        IsDeleted = false
                    }
                },
                new List<Enrollment>
                {
                    new Enrollment
                    {
                        Id = enrollmentId,
                        CourseId = courseId
                    }
                },
                new List<LessonProgress>
                {
                    new LessonProgress
                    {
                        EnrollmentId = enrollmentId,
                        LessonId = lessonId,
                        WatchPercentage = 80,
                        LastPosition = 120,
                        TimeSpentMinutes = 15,
                        Status = "InProgress",
                        StartedAt = DateTime.UtcNow.AddMinutes(-30)
                    }
                }
            );

            var query = new GetLessonProgressByEnrollmentQuery
            {
                EnrollmentId = enrollmentId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);

            var lesson = result.First();

            lesson.WatchPercentage.Should().Be(80);
            lesson.LastPosition.Should().Be(120);
            lesson.TimeSpentMinutes.Should().Be(15);
            lesson.Status.Should().Be("InProgress");
        }

        [Fact]
        public async Task Handle_NoProgress_DefaultStatusNotStarted()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();

            SetupData(
                new List<Lesson>
                {
                    new Lesson
                    {
                        Id = lessonId,
                        CourseId = courseId,
                        LessonTitle = "Lesson 1",
                        OrderIndex = 1,
                        IsDeleted = false
                    }
                },
                new List<Enrollment>
                {
                    new Enrollment
                    {
                        Id = enrollmentId,
                        CourseId = courseId
                    }
                },
                new List<LessonProgress>()
            );

            var query = new GetLessonProgressByEnrollmentQuery
            {
                EnrollmentId = enrollmentId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Status.Should().Be("NotStarted");
        }

        [Fact]
        public async Task Handle_ShouldReturnLessonsOrderedByOrderIndex()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();

            SetupData(
                new List<Lesson>
                {
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = courseId,
                        LessonTitle = "Lesson 2",
                        OrderIndex = 2,
                        IsDeleted = false
                    },
                    new Lesson
                    {
                        Id = Guid.NewGuid(),
                        CourseId = courseId,
                        LessonTitle = "Lesson 1",
                        OrderIndex = 1,
                        IsDeleted = false
                    }
                },
                new List<Enrollment>
                {
                    new Enrollment
                    {
                        Id = enrollmentId,
                        CourseId = courseId
                    }
                },
                new List<LessonProgress>()
            );

            var query = new GetLessonProgressByEnrollmentQuery
            {
                EnrollmentId = enrollmentId
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result[0].LessonTitle.Should().Be("Lesson 1");
            result[1].LessonTitle.Should().Be("Lesson 2");
        }
    }
}