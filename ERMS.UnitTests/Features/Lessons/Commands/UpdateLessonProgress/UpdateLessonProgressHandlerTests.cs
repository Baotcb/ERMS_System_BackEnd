using ERMS.Application.Features.Lessons.Commands.UpdateLessonProgress;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Lessons.Commands.UpdateLessonProgress
{
    public class UpdateLessonProgressHandlerTests : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly UpdateLessonProgressHandler _handler;
        private readonly Guid _userId;
        private readonly Guid _employeeId;

        public UpdateLessonProgressHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                // Thêm dòng này để bỏ qua lỗi Transaction
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _userId = Guid.NewGuid();
            _employeeId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);

            _handler = new UpdateLessonProgressHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_CompleteLesson_WhenWatchPercentage100()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();

            // Cập nhật: Thêm các trường Required cho Employee
            var employee = new Employee
            {
                Id = _employeeId,
                UserId = _userId,
                EmployeeCode = "EMP001", // Trường bị thiếu dẫn đến lỗi
               
            };

            var course = new Course
            {
                Id = courseId,
                CourseName = "Test Course",
                CourseCode = "C001", // Đảm bảo thêm các trường Required của Course
                TrainerEmail = "trainer@gmail.com"
            };

            var lesson = new Lesson
            {
                Id = lessonId,
                CourseId = courseId,
                IsDeleted = false,
                LessonTitle = "Lesson 1"
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                EmployeeId = _employeeId,
                CourseId = courseId
            };

            var progress = new LessonProgress
            {
                EnrollmentId = enrollmentId,
                LessonId = lessonId,
                Status = "In Progress",
                WatchPercentage = 10
            };

            _context.Employees.Add(employee);
            _context.Courses.Add(course);
            _context.Lessons.Add(lesson);
            _context.Enrollments.Add(enrollment);
            _context.LessonProgresses.Add(progress);

            // Lỗi xảy ra tại đây vì dữ liệu không hợp lệ
            await _context.SaveChangesAsync();

            var command = new UpdateLessonProgressCommand
            {
                LessonId = lessonId,
                WatchPercentage = 100,
                TimeSpentMinutes = 20
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            var updatedProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(x => x.LessonId == lessonId && x.EnrollmentId == enrollmentId);

            updatedProgress!.Status.Should().Be("Completed");
        }

        [Fact]
        public async Task Handle_EnrollmentNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Có lesson nhưng ko có enrollment
            var lessonId = Guid.NewGuid();
            _context.Lessons.Add(new Lesson { Id = lessonId, CourseId = Guid.NewGuid(), LessonTitle = "title" });
            await _context.SaveChangesAsync();

            var command = new UpdateLessonProgressCommand { LessonId = lessonId };

            // Act & Assert
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}