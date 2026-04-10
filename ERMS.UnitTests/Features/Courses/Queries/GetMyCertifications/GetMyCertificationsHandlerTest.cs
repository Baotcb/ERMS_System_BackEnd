using ERMS.Application.Features.Certifications.Queries;
using ERMS.Application.Features.Certifications.Queries.GetMyCertifications;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
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

namespace ERMS.UnitTests.Features.Certifications.Queries
{
    public class GetMyCertificationsHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetMyCertificationsHandler _handler;

        public GetMyCertificationsHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetMyCertificationsHandler(
                _context,
                _currentUserServiceMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetMyCertificationsHandler>>());
        }

        //   Helper chuẩn để tránh lỗi Required fields
        private Employee CreateEmployee(Guid userId, string fullName = "Test User")
        {
            var user = new User
            {
                Id = userId,
                FullName = fullName
            };

            return new Employee
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                User = user
            };
        }

        private Course CreateCourse(string name = "C# Basic", string code = "C01")
        {
            return new Course
            {
                Id = Guid.NewGuid(),
                CourseName = name,
                CourseCode = code,
                TrainerEmail = "trainer@erms.com",
                Status = "Published",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        private Enrollment CreateEnrollment(Employee employee, Course course)
        {
            return new Enrollment
            {
                Id = Guid.NewGuid(),
                Employee = employee,
                EmployeeId = employee.Id,
                Course = course,
                CourseId = course.Id,
                CertificateUrl = "certificate.pdf",
                CertificateIssuedAt = DateTime.UtcNow,
                Progress = 100,
                Status = "Completed",
                IsDeleted = false
            };
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUserIdIsNull()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
            var query = new GetMyCertificationsQuery();

            // Act & Assert
            await _handler.Invoking(x => x.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Không xác định được người dùng*");
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenEmployeeNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var query = new GetMyCertificationsQuery();

            // Act & Assert
            await _handler.Invoking(x => x.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("*Không tìm thấy nhân viên*");
        }

        [Fact]
        public async Task Handle_ShouldReturnCertifications_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var employee = CreateEmployee(userId);
            var user = employee.User;

            var course = CreateCourse();
            var enrollment = CreateEnrollment(employee, course);

            _context.Users.Add(user);
            _context.Employees.Add(employee);
            _context.Courses.Add(course);
            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();

            var query = new GetMyCertificationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].CourseName.Should().Be("C# Basic");
            result[0].EmployeeName.Should().Be("Test User");
            result[0].CertificateUrl.Should().Be("certificate.pdf");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}