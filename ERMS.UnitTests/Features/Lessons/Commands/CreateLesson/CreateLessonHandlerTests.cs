using ERMS.Application.Features.Lessons.Commands.CreateLesson;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Lessons.Commands.CreateLesson
{
    public class CreateLessonCommandHandlerTests : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly CreateLessonHandler _handler;

        public CreateLessonCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserMock = new Mock<ICurrentUserService>();

            _handler = new CreateLessonHandler(_context, _currentUserMock.Object);
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
        public async Task Handle_Should_Create_TextLesson_Success()
        {
            var enterpriseId = Guid.NewGuid();
            var course = CreateCourse(enterpriseId);

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            SetupUser(enterpriseId);

            var command = new CreateLessonCommand
            {
                CourseId = course.Id,
                LessonTitle = "Text lesson",
                OrderIndex = 1,
                ContentType = "Text",
                Content = "Hello world"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            var lesson = await _context.Lessons.FindAsync(result);

            lesson.Should().NotBeNull();
            lesson!.Content.Should().Be("Hello world");
        }

        [Fact]
        public async Task Handle_Should_Create_VideoLesson_Success()
        {
            var enterpriseId = Guid.NewGuid();
            var course = CreateCourse(enterpriseId);

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            SetupUser(enterpriseId);

            var command = new CreateLessonCommand
            {
                CourseId = course.Id,
                LessonTitle = "Video lesson",
                OrderIndex = 1,
                ContentType = "Video",
                VideoUrl = "http://video.com"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            var lesson = await _context.Lessons.FindAsync(result);

            lesson!.VideoUrl.Should().Be("http://video.com");
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Course_NotFound()
        {
            SetupUser(Guid.NewGuid());

            var command = new CreateLessonCommand
            {
                CourseId = Guid.NewGuid(),
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

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            SetupUser(Guid.NewGuid(), "User", "abc@test.com");

            var command = new CreateLessonCommand
            {
                CourseId = course.Id,
                LessonTitle = "Test",
                OrderIndex = 1
            };

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}