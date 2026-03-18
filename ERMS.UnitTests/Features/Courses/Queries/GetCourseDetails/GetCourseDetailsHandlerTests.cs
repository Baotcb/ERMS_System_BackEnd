using ERMS.Application.Features.Courses.Queries.GetCourseDetails;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Skill;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using CourseEntity = ERMS.Domain.Entities.Training.Course;

namespace ERMS.UnitTests.Features.Courses.Queries.GetCourseDetails
{
    public class GetCourseDetailsHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetCourseDetailsHandler _handler;

        public GetCourseDetailsHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _handler = new GetCourseDetailsHandler(_context, _currentUserServiceMock.Object);
        }

        private CourseEntity CreateValidCourse(Guid enterpriseId, Guid courseId)
        {
            return new CourseEntity
            {
                Id = courseId,
                EnterpriseId = enterpriseId,
                CourseName = "Advanced .NET",
                CourseCode = "DOTNET-ADV",
                TrainerEmail = "trainer@company.com",
                Status = "Published",
                IsDeleted = false,
                CompletionCriteria = "Watch 100% videos",
                CreatedAt = DateTime.UtcNow,
                Lessons = new List<Lesson>(),
                Enrollments = new List<Enrollment>(),
                CourseSkills = new List<CourseSkill>()
            };
        }

        [Fact]
        public async Task Handle_ShouldReturnDetails_WhenCourseExists()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var course = CreateValidCourse(enterpriseId, courseId);

            // Thêm Lesson hợp lệ
            course.Lessons.Add(new Lesson
            {
                Id = Guid.NewGuid(),
                LessonTitle = "Giới thiệu",
                IsDeleted = false
            });

            // Thêm Skill thông qua bảng trung gian
            var skill = new Skill { Id = Guid.NewGuid(), SkillName = "C# Programming" };
            _context.Skills.Add(skill);
            course.CourseSkills.Add(new CourseSkill { CourseId = courseId, SkillId = skill.Id });

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var query = new GetCourseDetailsQuery { Id = courseId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(courseId);
            result.CourseName.Should().Be("Advanced .NET");
            result.LessonCount.Should().Be(1);
            result.Skills.Should().Contain("C# Programming");
        }

        [Fact]
        public async Task Handle_ShouldThrowKeyNotFoundException_WhenCourseDoesNotExist()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var query = new GetCourseDetailsQuery { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Không tìm thấy khóa học.");
        }

        [Fact]
        public async Task Handle_ShouldThrowKeyNotFoundException_WhenCourseIsDeleted()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var course = CreateValidCourse(enterpriseId, courseId);
            course.IsDeleted = true; // Đánh dấu đã xóa

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var query = new GetCourseDetailsQuery { Id = courseId };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenEnterpriseIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var query = new GetCourseDetailsQuery { Id = Guid.NewGuid() };

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}