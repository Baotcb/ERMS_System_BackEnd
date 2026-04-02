using ERMS.Application.Features.Courses.Commands.UpdateCourse;
using ERMS.Application.Interface;
using CourseEntity = ERMS.Domain.Entities.Training.Course;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Courses.Commands.UpdateCourse
{
    public class UpdateCourseHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<UpdateCourseHandler>> _loggerMock;
        private readonly UpdateCourseHandler _handler;

        public UpdateCourseHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<UpdateCourseHandler>>();

            _handler = new UpdateCourseHandler(
                _context,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private CourseEntity CreateBaseCourse(Guid id, Guid enterpriseId, string code)
        {
            return new CourseEntity
            {
                Id = id,
                EnterpriseId = enterpriseId,
                CourseName = "Original Name",
                CourseCode = code,
                TrainerEmail = "original@test.com",
                Status = "Draft",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task Handle_ShouldUpdateCourseSuccessfully()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var existingCourse = CreateBaseCourse(courseId, enterpriseId, "OLD-CODE");
            _context.Courses.Add(existingCourse);
            await _context.SaveChangesAsync();

            var command = new UpdateCourseCommand
            {
                Id = courseId,
                CourseName = "Updated Name",
                CourseCode = "NEW-CODE",
                TrainerEmail = "updated@test.com",
                IsOnline = true,
                CompletionCriteria = "Pass All Quizzes"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(courseId);
            var updatedCourse = await _context.Courses.FindAsync(courseId);
            updatedCourse!.CourseName.Should().Be("Updated Name");
            updatedCourse.CourseCode.Should().Be("NEW-CODE");
            updatedCourse.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCourseNotFoundOrDeleted()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

            var command = new UpdateCourseCommand { Id = Guid.NewGuid(), CourseName = "Test" };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy khóa học.");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenNewCodeAlreadyExistsInOtherCourse()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var course1Id = Guid.NewGuid();
            var course2Id = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Courses.Add(CreateBaseCourse(course1Id, enterpriseId, "CODE-1"));
            _context.Courses.Add(CreateBaseCourse(course2Id, enterpriseId, "CODE-2"));
            await _context.SaveChangesAsync();

            // Cố gắng cập nhật Course 1 nhưng lấy CourseCode của Course 2
            var command = new UpdateCourseCommand
            {
                Id = course1Id,
                CourseCode = "CODE-2",
                CourseName = "Attempt to steal code",
                TrainerEmail = "test@test.com"
            };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Mã khóa học đã tồn tại.");
        }

        [Fact]
        public async Task Handle_ShouldNotThrow_WhenUpdatingTheSameCourseWithItsOwnCode()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Courses.Add(CreateBaseCourse(courseId, enterpriseId, "MY-CODE"));
            await _context.SaveChangesAsync();

            var command = new UpdateCourseCommand
            {
                Id = courseId,
                CourseCode = "MY-CODE", // Vẫn giữ nguyên code cũ
                CourseName = "Just updating name",
                TrainerEmail = "test@test.com"
            };

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorized_WhenUserNotLoggedIn()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new UpdateCourseCommand { Id = Guid.NewGuid() };

            // Act & Assert
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