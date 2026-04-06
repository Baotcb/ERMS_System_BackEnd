using ERMS.Application.Features.Enrollments.Queries.GetTrainingPerformanceChart;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Enrollments.Queries.GetTrainingPerformanceChart
{
    public class GetTrainingPerformanceChartHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<GetTrainingPerformanceChartHandler>> _loggerMock;
        private readonly GetTrainingPerformanceChartHandler _handler;

        public GetTrainingPerformanceChartHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<GetTrainingPerformanceChartHandler>>();

            _handler = new GetTrainingPerformanceChartHandler(
                _contextMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        private void SetupMockContext(List<Enrollment> enrollments)
        {
            var dbSetMock = enrollments.AsQueryable().BuildMockDbSet();
            _contextMock.Setup(x => x.Enrollments).Returns(dbSetMock.Object);
        }

        [Fact]
        public async Task Handle_EnterpriseIdNull_ThrowsException()
        {
            // Arrange
            _currentUserServiceMock
                .Setup(x => x.GetEnterpriseIdAsync())
                .ReturnsAsync((Guid?)null);

            var query = new GetTrainingPerformanceChartQuery();

            // Act & Assert
            var act = async () => await _handler.Handle(query, CancellationToken.None);
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Người dùng không thuộc doanh nghiệp nào.");
        }

        [Fact]
        public async Task Handle_GroupByCourseName_CalculatesCorrectPercentage()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var courseA = new Course { CourseName = "Course A", EnterpriseId = enterpriseId };
            var courseB = new Course { CourseName = "Course B", EnterpriseId = enterpriseId };

            var enrollments = new List<Enrollment>
            {
                // Course A: 2 Passed / 3 Total = 67%
                new Enrollment { Course = courseA, Status = "Completed", Progress = 100, IsDeleted = false },
                new Enrollment { Course = courseA, Status = "InProgress", Progress = 100, IsDeleted = false }, // Passed by Progress
                new Enrollment { Course = courseA, Status = "InProgress", Progress = 50, IsDeleted = false },
                
                // Course B: 1 Passed / 2 Total = 50%
                new Enrollment { Course = courseB, Status = "Completed", Progress = 100, IsDeleted = false },
                new Enrollment { Course = courseB, Status = "NotStarted", Progress = 0, IsDeleted = false }
            };

            SetupMockContext(enrollments);

            var query = new GetTrainingPerformanceChartQuery { GroupByLevel = false };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.First(x => x.Label == "Course A").Value.Should().Be(67);
            result.First(x => x.Label == "Course B").Value.Should().Be(50);
        }

        [Fact]
        public async Task Handle_GroupByLevel_CalculatesCorrectPercentage()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var courseBasic = new Course { Level = "Basic", EnterpriseId = enterpriseId };
            var courseAdvanced = new Course { Level = "Advanced", EnterpriseId = enterpriseId };

            var enrollments = new List<Enrollment>
            {
                // Basic Level: 1 Passed / 1 Total = 100%
                new Enrollment { Course = courseBasic, Status = "Completed", Progress = 100 },
                
                // Advanced Level: 0 Passed / 1 Total = 0%
                new Enrollment { Course = courseAdvanced, Status = "InProgress", Progress = 10 }
            };

            SetupMockContext(enrollments);

            var query = new GetTrainingPerformanceChartQuery { GroupByLevel = true };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.First(x => x.Label == "Basic").Value.Should().Be(100);
            result.First(x => x.Label == "Advanced").Value.Should().Be(0);
        }

        [Fact]
        public async Task Handle_FiltersDeletedRecords_AndOtherEnterprise()
        {
            // Arrange
            var myEnterpriseId = Guid.NewGuid();
            var otherEnterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(myEnterpriseId);

            var myCourse = new Course { CourseName = "My Course", EnterpriseId = myEnterpriseId };
            var otherCourse = new Course { CourseName = "Other Course", EnterpriseId = otherEnterpriseId };

            var enrollments = new List<Enrollment>
            {
                // Hợp lệ
                new Enrollment { Course = myCourse, Status = "Completed", Progress = 100, IsDeleted = false },
                // Bị xóa mềm
                new Enrollment { Course = myCourse, IsDeleted = true },
                // Thuộc doanh nghiệp khác
                new Enrollment { Course = otherCourse, Status = "Completed", Progress = 100 }
            };

            SetupMockContext(enrollments);

            // Act
            var result = await _handler.Handle(new GetTrainingPerformanceChartQuery(), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Label.Should().Be("My Course");
            result[0].Value.Should().Be(100); // Chỉ tính 1 bản ghi hợp lệ duy nhất
        }
    }
}