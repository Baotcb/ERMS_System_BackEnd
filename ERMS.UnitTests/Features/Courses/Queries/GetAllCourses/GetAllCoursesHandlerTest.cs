using ERMS.Application.Features.Courses.Queries.GetAllCourses;
using ERMS.Application.Interface;
using CourseEntity = ERMS.Domain.Entities.Training.Course; // Alias để tránh lỗi "namespace vs type"
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Courses.Queries.GetAllCourses
{
    public class GetAllCoursesHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetAllCoursesHandler _handler;

        public GetAllCoursesHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new GetAllCoursesHandler(_context, _currentUserServiceMock.Object);
        }

        // Helper method để tạo Course hợp lệ, tránh lỗi Required properties
        private CourseEntity CreateCourse(Guid enterpriseId, string name, string code, string status = "Published", bool isMandatory = false)
        {
            return new CourseEntity
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CourseName = name,
                CourseCode = code,
                TrainerEmail = "trainer@erms.com", // Luôn có giá trị mặc định cho test
                Status = status,
                IsMandatory = isMandatory,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotInEnterprise()
        {
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var query = new GetAllCourseQuery();

            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldReturnAllCourses_AndOrderDescendingByCreatedAt()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var c1 = CreateCourse(enterpriseId, "Older Course", "C1");
            c1.CreatedAt = DateTime.UtcNow.AddDays(-1);

            var c2 = CreateCourse(enterpriseId, "Newer Course", "C2");
            c2.CreatedAt = DateTime.UtcNow;

            _context.Courses.AddRange(c1, c2);
            await _context.SaveChangesAsync();

            var query = new GetAllCourseQuery { Page = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(2);
            result.Items.First().CourseCode.Should().Be("C2"); // Mới nhất lên đầu
        }

        [Fact]
        public async Task Handle_ShouldFilterBySearch_WhenProvided()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            _context.Courses.Add(CreateCourse(enterpriseId, "React Mastery", "FE-01"));
            _context.Courses.Add(CreateCourse(enterpriseId, "SQL Server", "DB-01"));
            await _context.SaveChangesAsync();

            var query = new GetAllCourseQuery { Search = "react" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(x => x.CourseCode == "FE-01");
        }

        [Fact]
        public async Task Handle_ShouldReturnCorrectCounts_ForLessonsAndEnrollments()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var course = CreateCourse(enterpriseId, "Fullstack", "FS-01");

            // Cập nhật: Thêm LessonTitle cho từng Lesson
            course.Lessons = new List<Lesson>
    {
        new Lesson
        {
            Id = Guid.NewGuid(),
            LessonTitle = "Bài học 1", // <--- Thêm trường bắt buộc
            IsDeleted = false
        },
        new Lesson
        {
            Id = Guid.NewGuid(),
            LessonTitle = "Bài học cũ", // <--- Thêm trường bắt buộc
            IsDeleted = true
        }
    };

            course.Enrollments = new List<Enrollment>
    {
        new Enrollment
        {
            Id = Guid.NewGuid(),
            IsDeleted = false 
            // Nếu Enrollment báo lỗi thiếu trường, hãy thêm các trường bắt buộc của nó vào đây
        }
    };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var query = new GetAllCourseQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var item = result.Items.First();
            item.LessonCount.Should().Be(1); // Chỉ đếm IsDeleted = false
            item.EnrollmentCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ShouldApplyPaginationCorrectly()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            for (int i = 1; i <= 5; i++)
            {
                _context.Courses.Add(CreateCourse(enterpriseId, $"Course {i}", $"C{i}"));
            }
            await _context.SaveChangesAsync();

            var query = new GetAllCourseQuery { Page = 2, PageSize = 2 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(5);
            result.Items.Should().HaveCount(2);
            result.Page.Should().Be(2);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}