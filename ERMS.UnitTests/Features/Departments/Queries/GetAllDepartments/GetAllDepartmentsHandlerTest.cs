using ERMS.Application.Features.Departments.Queries.GetAllDepartments;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Organization;
using ERMS.Infrastructure.Data;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Departments.Queries.GetAllDepartments
{
    public class GetAllDepartmentsHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly GetAllDepartmentsHandler _handler;

        public GetAllDepartmentsHandlerTest()
        {
            // Use InMemory Database for queries to support .CountAsync() properly with Mock setups if complex
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _currentUserServiceMock = new Mock<ICurrentUserService>();

            _handler = new GetAllDepartmentsHandler(_context, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotInEnterprise()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync((Guid?)null);
            var query = new GetAllDepartmentsQuery();

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("User does not belong to any enterprise");
        }

        [Fact]
        public async Task Handle_ShouldReturnAllDepartments_WhenNoFilters()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var d1 = new Department { Id = 1, EnterpriseId = enterpriseId, DepartmentName = "HR", IsDeleted = false };
            var d2 = new Department { Id = 2, EnterpriseId = enterpriseId, DepartmentName = "IT", IsDeleted = false };
            var d3 = new Department { Id = 3, EnterpriseId = enterpriseId, DepartmentName = "Deleted", IsDeleted = true };
            
            _context.Departments.AddRange(d1, d2, d3);
            await _context.SaveChangesAsync();

            var query = new GetAllDepartmentsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items.Select(i => i.DepartmentName).Should().Contain(new[] { "HR", "IT" });
        }

        [Fact]
        public async Task Handle_ShouldReturnFilteredDepartments_WhenSearchByCodeOrName()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var d1 = new Department { Id = 1, EnterpriseId = enterpriseId, DepartmentName = "Human Resources", DepartmentCode = "HR", IsDeleted = false };
            var d2 = new Department { Id = 2, EnterpriseId = enterpriseId, DepartmentName = "IT Dept", DepartmentCode = "IT", IsDeleted = false };
            
            _context.Departments.AddRange(d1, d2);
            await _context.SaveChangesAsync();

            var query = new GetAllDepartmentsQuery { Search = "Human" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().DepartmentName.Should().Be("Human Resources");
        }

        [Fact]
        public async Task Handle_ShouldReturnFilteredDepartments_WhenFilterByIsActive()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            var d1 = new Department { Id = 1, EnterpriseId = enterpriseId, DepartmentName = "Active Dept", IsActive = true, IsDeleted = false };
            var d2 = new Department { Id = 2, EnterpriseId = enterpriseId, DepartmentName = "Inactive Dept", IsActive = false, IsDeleted = false };
            
            _context.Departments.AddRange(d1, d2);
            await _context.SaveChangesAsync();

            var query = new GetAllDepartmentsQuery { IsActive = false };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().DepartmentName.Should().Be("Inactive Dept");
        }

        [Fact]
        public async Task Handle_ShouldApplyPagination_WhenRequested()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserServiceMock.Setup(x => x.GetEnterpriseIdAsync()).ReturnsAsync(enterpriseId);

            for(int i = 1; i <= 5; i++)
            {
                _context.Departments.Add(new Department { Id = i, EnterpriseId = enterpriseId, DepartmentName = $"Dept {i}", IsDeleted = false });
            }
            await _context.SaveChangesAsync();

            var query = new GetAllDepartmentsQuery { Page = 2, PageSize = 2 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(5);
            result.Items.Should().HaveCount(2); // Since there are 5, page 2 with size 2 should have 2 items.
            // Items are ordered by DepartmentName: Dept 1, Dept 2, Dept 3, Dept 4, Dept 5
            // Page 2 should have Dept 3 and Dept 4
            result.Items.Select(i => i.DepartmentName).Should().Contain(new[] { "Dept 3", "Dept 4" });
        }
    }
}
