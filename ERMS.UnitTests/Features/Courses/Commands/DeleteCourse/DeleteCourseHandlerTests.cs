using ERMS.Application.Features.Courses.Commands.DeleteCourse;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Courses.Commands.DeleteCourse
{
    public class DeleteCourseHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<DeleteCourseHandler>> _loggerMock;
        private readonly DeleteCourseHandler _handler;

        public DeleteCourseHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<DeleteCourseHandler>>();

            _handler = new DeleteCourseHandler(
                _context,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldDeleteCourseSuccessfully()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                EnterpriseId = enterpriseId,
                CourseName = "Test",
                CourseCode = "C01",
                TrainerEmail = "trainer@test.com",
                Status = "Draft",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new[] { "HR" });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var result = await _handler.Handle(new DeleteCourseCommand { Id = courseId }, CancellationToken.None);

            result.Should().Be(courseId);

            var deleted = await _context.Courses.FindAsync(courseId);
            deleted!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenHasEnrollment()
        {
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                EnterpriseId = enterpriseId,
                CourseName = "Test",
                CourseCode = "C01",
                TrainerEmail = "trainer@test.com",
                Status = "Draft",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            _context.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                EmployeeId = Guid.NewGuid()
            });

            await _context.SaveChangesAsync();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.Roles).Returns(new[] { "HR" });
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var act = () => _handler.Handle(new DeleteCourseCommand { Id = courseId }, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không thể xóa khóa học đã có học viên.");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}