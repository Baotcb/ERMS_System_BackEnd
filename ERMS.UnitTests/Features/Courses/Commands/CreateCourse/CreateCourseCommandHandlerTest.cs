using ERMS.Application.Features.Courses.Commands.CreateCourse;
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

namespace ERMS.UnitTests.Features.Courses.Commands.CreateCourse
{
    public class CreateCourseCommandHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<CreateCourseCommandHandler>> _loggerMock;
        private readonly CreateCourseCommandHandler _handler;

        public CreateCourseCommandHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<CreateCourseCommandHandler>>();

            _handler = new CreateCourseCommandHandler(
                _context,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateCourseSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var command = new CreateCourseCommand
            {
                CourseName = "Lập trình C# cơ bản",
                CourseCode = "CS101",
                TrainerEmail = "trainer@erms.com",
                StartTime = DateTime.UtcNow.AddDays(7),
                IsOnline = true,
                CompletionCriteria = "Pass Final Quiz"
            };

            // Act
            var resultId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultId.Should().NotBeEmpty();

            var courseInDb = await _context.Courses.FirstOrDefaultAsync(x => x.Id == resultId);
            courseInDb.Should().NotBeNull();
            courseInDb!.CourseCode.Should().Be("CS101");
            courseInDb.EnterpriseId.Should().Be(enterpriseId);
            courseInDb.Status.Should().Be("Draft");

            // Verify logger was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("created by User")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenCourseCodeAlreadyExistsInSameEnterprise()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            // Seed an existing course
            _context.Courses.Add(new Course
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CourseCode = "EXISTING-001",
                CourseName = "Existing Course",
                TrainerEmail = "test@test.com",
                Status = "Published",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();

            var command = new CreateCourseCommand
            {
                CourseCode = "EXISTING-001", // Trùng mã
                CourseName = "New Course",
                TrainerEmail = "new@test.com"
            };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Mã khóa học đã tồn tại");
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var command = new CreateCourseCommand { CourseName = "Test" };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEnterpriseIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var command = new CreateCourseCommand { CourseName = "Test" };

            // Act & Assert
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào");
        }

        [Fact]
        public async Task Handle_ShouldSetLocationToNull_WhenIsOnlineIsTrue()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var command = new CreateCourseCommand
            {
                CourseName = "Online Course",
                CourseCode = "ONL-01",
                IsOnline = true,
                Location = "Phòng họp A", // Sẽ bị null vì IsOnline = true
                TrainerEmail = "t@t.com"
            };

            // Act
            var resultId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var courseInDb = await _context.Courses.FindAsync(resultId);
            courseInDb!.Location.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenStartTimeAlreadyExistsInSameEnterprise()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var startTime = DateTime.UtcNow.AddDays(5);

            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            // Seed course có cùng StartTime
            _context.Courses.Add(new Course
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CourseCode = "COURSE-001",
                CourseName = "Existing Course",
                TrainerEmail = "test@test.com",
                StartTime = startTime,
                Status = "Published",
                IsDeleted = false
            });

            await _context.SaveChangesAsync();

            var command = new CreateCourseCommand
            {
                CourseCode = "NEW-001",
                CourseName = "New Course",
                TrainerEmail = "new@test.com",
                StartTime = startTime //   trùng thời gian
            };

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Thời gian học bị trùng");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}