using ERMS.Application.Features.Lessons.Commands.UpdateLesson;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Lessons.Commands.UpdateLesson
{
    public class UpdateLessonHandlerTests : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly UpdateLessonHandler _handler;

        public UpdateLessonHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserMock = new Mock<ICurrentUserService>();

            _handler = new UpdateLessonHandler(_context, _currentUserMock.Object);
        }

        private Course CreateCourse(Guid enterpriseId)
        {
            return new Course
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CourseCode = "COURSE-001",
                CourseName = "Test Course",
                TrainerEmail = "trainer@test.com",
                StartTime = DateTime.UtcNow.AddDays(1),
                CompletionCriteria = "Quiz",
                Status = "Draft",
                IsOnline = true,
                IsMandatory = false,
                IsDeleted = false
            };
        }

        private Lesson CreateLesson(Guid courseId)
        {
            return new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                LessonTitle = "Old",
                OrderIndex = 1,
                ContentType = "Text",
                Content = "Old content",
                IsDeleted = false
            };
        }

        private void SetupUser(Guid enterpriseId, string role = "HR", string email = "admin@test.com")
        {
            _currentUserMock.Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync(enterpriseId);
            _currentUserMock.Setup(x => x.Roles)
                .Returns(new[] { role });
            _currentUserMock.Setup(x => x.Email)
                .Returns(email);
        }

        [Fact]
        public async Task Handle_Should_Update_TextLesson_Success()
        {
            var enterpriseId = Guid.NewGuid();
            var course = CreateCourse(enterpriseId);
            var lesson = CreateLesson(course.Id);

            _context.Courses.Add(course);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            SetupUser(enterpriseId);

            var command = new UpdateLessonCommand
            {
                Id = lesson.Id,
                LessonTitle = "Updated",
                OrderIndex = 2,
                ContentType = "Text",
                Content = "New content"
            };

            await _handler.Handle(command, CancellationToken.None);

            var updated = await _context.Lessons.FindAsync(lesson.Id);

            updated!.LessonTitle.Should().Be("Updated");
            updated.Content.Should().Be("New content");
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Lesson_NotFound()
        {
            SetupUser(Guid.NewGuid());

            var command = new UpdateLessonCommand
            {
                Id = Guid.NewGuid(),
                LessonTitle = "Test",
                OrderIndex = 1
            };

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_When_NoPermission()
        {
            var course = CreateCourse(Guid.NewGuid());
            var lesson = CreateLesson(course.Id);

            _context.Courses.Add(course);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            SetupUser(Guid.NewGuid(), "User", "abc@test.com");

            var command = new UpdateLessonCommand
            {
                Id = lesson.Id,
                LessonTitle = "Update",
                OrderIndex = 1
            };

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Missing_VideoUrl()
        {
            var enterpriseId = Guid.NewGuid();
            var course = CreateCourse(enterpriseId);
            var lesson = CreateLesson(course.Id);

            _context.Courses.Add(course);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            SetupUser(enterpriseId);

            var command = new UpdateLessonCommand
            {
                Id = lesson.Id,
                LessonTitle = "Video",
                OrderIndex = 1,
                ContentType = "Video",
                VideoUrl = null
            };

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}