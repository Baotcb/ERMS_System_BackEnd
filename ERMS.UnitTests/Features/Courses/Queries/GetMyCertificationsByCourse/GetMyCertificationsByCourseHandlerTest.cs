using ERMS.Application.Features.Certifications.Queries;
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
    public class GetMyCertificationsByCourseHandlerTest : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetMyCertificationsByCourseHandler _handler;

        public GetMyCertificationsByCourseHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetMyCertificationsByCourseHandler(
                _context,
                _currentUserServiceMock.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetMyCertificationsByCourseHandler>>());
        }

        //   Helper tạo Employee chuẩn (tránh lỗi thiếu field)
        private Employee CreateEmployee(Guid userId, string name = "User A")
        {
            return new Employee
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N").Substring(0, 6), //   FIX
                User = new User
                {
                    Id = userId,
                    FullName = name
                }
            };
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyMatchingCourse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var employee = CreateEmployee(userId);
            var user = employee.User;

            var course1 = new Course
            {
                Id = Guid.NewGuid(),
                CourseName = "Course 1",
                CourseCode = "C1",
                TrainerEmail = "t@test.com",
                Status = "Published"
            };

            var course2 = new Course
            {
                Id = Guid.NewGuid(),
                CourseName = "Course 2",
                CourseCode = "C2",
                TrainerEmail = "t@test.com",
                Status = "Published"
            };

            _context.Users.Add(user);
            _context.Employees.Add(employee);
            _context.Courses.AddRange(course1, course2);

            _context.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                Employee = employee,
                EmployeeId = employee.Id,
                Course = course1,
                CourseId = course1.Id,
                CertificateUrl = "cert1.pdf",
                Progress = 100,
                Status = "Completed",
                IsDeleted = false
            });

            _context.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                Employee = employee,
                EmployeeId = employee.Id,
                Course = course2,
                CourseId = course2.Id,
                CertificateUrl = "cert2.pdf",
                Progress = 100,
                Status = "Completed",
                IsDeleted = false
            });

            await _context.SaveChangesAsync();

            var query = new GetMyCertificationsByCourseQuery
            {
                CourseId = course1.Id
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].CourseId.Should().Be(course1.Id);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}