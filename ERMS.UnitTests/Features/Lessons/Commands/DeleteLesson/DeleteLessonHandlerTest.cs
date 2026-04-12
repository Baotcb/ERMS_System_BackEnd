using ERMS.Application.Features.Lessons.Commands.DeleteLesson;
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

namespace ERMS.UnitTests.Features.Lessons.Commands.DeleteLesson
{
    public class DeleteLessonHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly DeleteLessonHandler _handler;

        public DeleteLessonHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new DeleteLessonHandler(
                _context,
                _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldDeleteLessonSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();

            var course = new Course
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                ContentManagerEmail = "user@test.com",
                                CourseName = "Test Course",
                CourseCode = "C01",
                TrainerEmail = "trainer@test.com",
                Status = "Draft",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var lesson = new Lesson
            {
                Id = Guid.NewGuid(),
                Course = course,
                    LessonTitle = "Test Lesson",
            };

            _context.Courses.Add(course);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            _currentUserServiceMock.Setup(x => x.Email).Returns("user@test.com");
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var result = await _handler.Handle(new DeleteLessonCommand { Id = lesson.Id }, CancellationToken.None);

            result.Should().Be(lesson.Id);
            lesson.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenHasProgress()
        {
            var enterpriseId = Guid.NewGuid();

            var course = new Course
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CourseName = "Test Course",
                CourseCode = "C01",
                TrainerEmail = "trainer@test.com",
                Status = "Draft",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
            var lesson = new Lesson { Id = Guid.NewGuid(), Course = course, LessonTitle = "Test Lesson" };

            _context.Lessons.Add(lesson);
            _context.LessonProgresses.Add(new LessonProgress
            {
                LessonId = lesson.Id,
                EnrollmentId = Guid.NewGuid()
            });

            await _context.SaveChangesAsync();

            _currentUserServiceMock.Setup(x => x.Email).Returns(course.ContentManagerEmail);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var act = () => _handler.Handle(new DeleteLessonCommand { Id = lesson.Id }, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không thể xóa bài học đã có tiến trình học");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}