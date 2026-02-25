using ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.JobPostings.Queries.GetPublicJobPostings
{
    public class GetPublicJobPostingsHandlerTest
    {
        private readonly ERMSDbContext _context;
        private readonly GetPublicJobPostingsHandler _handler;

        public GetPublicJobPostingsHandlerTest()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _handler = new GetPublicJobPostingsHandler(_context);
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyPublishedAndActiveJobs_WhenSuccessful()
        {
            // Arrange
            var activeEnterpriseId = Guid.NewGuid();
            var lockedEnterpriseId = Guid.NewGuid();
            var departmentId = 1;

            var department = new Department { Id = departmentId, DepartmentName = "Sales" };
            _context.Departments.Add(department);

            _context.Enterprises.AddRange(new List<Enterprise>
            {
                new Enterprise { Id = activeEnterpriseId, EnterpriseName = "Active Ent", EnterpriseCode = "A1", Status = "Active", SubscriptionPlanId = Guid.NewGuid() },
                new Enterprise { Id = lockedEnterpriseId, EnterpriseName = "Locked Ent", EnterpriseCode = "L1", Status = "Locked", SubscriptionPlanId = Guid.NewGuid() }
            });

            _context.JobPostings.AddRange(new List<JobPosting>
            {
                // Visible
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = activeEnterpriseId, JobTitle = "Public Job", Status = JobPostingStatus.Published, DepartmentId = departmentId, Description = "Desc", CreatedById = Guid.NewGuid(), CreatedAt = DateTime.UtcNow },
                // Hidden - Wrong status
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = activeEnterpriseId, JobTitle = "Draft Job", Status = JobPostingStatus.Draft, DepartmentId = departmentId, Description = "Desc", CreatedById = Guid.NewGuid() },
                // Hidden - Enterprise locked
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = lockedEnterpriseId, JobTitle = "Locked Ent Job", Status = JobPostingStatus.Published, DepartmentId = departmentId, Description = "Desc", CreatedById = Guid.NewGuid() },
                // Hidden - Expired
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = activeEnterpriseId, JobTitle = "Expired Job", Status = JobPostingStatus.Published, DepartmentId = departmentId, Description = "Desc", CreatedById = Guid.NewGuid(), ApplicationDeadline = DateTime.UtcNow.AddDays(-1) }
            });
            await _context.SaveChangesAsync();

            var query = new GetPublicJobPostingsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().JobTitle.Should().Be("Public Job");
        }

        [Fact]
        public async Task Handle_ShouldFilterBySearchTerm_WhenSuccessful()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var departmentId = 1;

            _context.Departments.Add(new Department { Id = departmentId, DepartmentName = "IT" });
            _context.Enterprises.Add(new Enterprise { Id = enterpriseId, EnterpriseName = "Ent 1", EnterpriseCode = "E1", Status = "Active", SubscriptionPlanId = Guid.NewGuid() });

            _context.JobPostings.AddRange(new List<JobPosting>
            {
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, JobTitle = "Software Engineer", Status = JobPostingStatus.Published, DepartmentId = departmentId, Description = "Code", CreatedById = Guid.NewGuid() },
                new JobPosting { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, JobTitle = "Accountant", Status = JobPostingStatus.Published, DepartmentId = departmentId, Description = "Numbers", CreatedById = Guid.NewGuid() }
            });
            await _context.SaveChangesAsync();

            var query = new GetPublicJobPostingsQuery { SearchTerm = "engineer", PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().JobTitle.Should().Be("Software Engineer");
        }
    }
}
