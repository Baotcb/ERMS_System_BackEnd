using ERMS.Application.Features.JobPostings.Queries.GetPublicJobPostings;
using ERMS.Application.Features.JobPostings.Queries.PublicJobFiltering;
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

        [Fact]
        public async Task Handle_ShouldSearchEnterpriseName_WhenSuccessful()
        {
            var enterpriseId = Guid.NewGuid();
            const int departmentId = 1;

            _context.Departments.Add(new Department { Id = departmentId, DepartmentName = "IT" });
            _context.Enterprises.Add(new Enterprise
            {
                Id = enterpriseId,
                EnterpriseName = "Amazing Tech",
                EnterpriseCode = "AT1",
                Status = "Active",
                SubscriptionPlanId = Guid.NewGuid()
            });

            _context.JobPostings.Add(new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                JobTitle = "Business Analyst",
                Status = JobPostingStatus.Published,
                DepartmentId = departmentId,
                Description = "Analyze requirements",
                CreatedById = Guid.NewGuid()
            });
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new GetPublicJobPostingsQuery
            {
                SearchTerm = "amazing",
                PageNumber = 1,
                PageSize = 10
            }, CancellationToken.None);

            result.Items.Should().ContainSingle();
            result.Items[0].EnterpriseName.Should().Be("Amazing Tech");
        }

        [Fact]
        public async Task Handle_ShouldApplyDepartmentSalaryExperienceAndEmploymentFilters()
        {
            var enterpriseId = Guid.NewGuid();
            const int salesDepartmentId = 1;
            const int engineeringDepartmentId = 2;

            _context.Departments.AddRange(
                new Department { Id = salesDepartmentId, DepartmentName = "Sales" },
                new Department { Id = engineeringDepartmentId, DepartmentName = "Engineering" });

            _context.Enterprises.Add(new Enterprise
            {
                Id = enterpriseId,
                EnterpriseName = "Filter Corp",
                EnterpriseCode = "FC1",
                Status = "Active",
                SubscriptionPlanId = Guid.NewGuid()
            });

            _context.JobPostings.AddRange(
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Senior Developer",
                    Status = JobPostingStatus.Published,
                    DepartmentId = engineeringDepartmentId,
                    Description = "Code",
                    ExperienceLevel = "3-5 years",
                    SalaryRangeMin = 20_000_000,
                    SalaryRangeMax = 30_000_000,
                    ShowSalary = true,
                    EmploymentType = "Full-time",
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow.AddDays(-1)
                },
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Sales Intern",
                    Status = JobPostingStatus.Published,
                    DepartmentId = salesDepartmentId,
                    Description = "Learn sales",
                    ExperienceLevel = "0-1 years",
                    SalaryRangeMin = 5_000_000,
                    SalaryRangeMax = 8_000_000,
                    ShowSalary = true,
                    EmploymentType = "Internship",
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow
                },
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Hidden Salary Developer",
                    Status = JobPostingStatus.Published,
                    DepartmentId = engineeringDepartmentId,
                    Description = "Code in secret",
                    ExperienceLevel = "3-5 years",
                    ShowSalary = false,
                    EmploymentType = "FullTime",
                    CreatedById = Guid.NewGuid()
                });
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new GetPublicJobPostingsQuery
            {
                DepartmentId = engineeringDepartmentId,
                EmploymentType = "FullTime",
                ExperienceBucket = "3-5",
                MinSalary = 18_000_000,
                MaxSalary = 32_000_000,
                PageNumber = 1,
                PageSize = 10
            }, CancellationToken.None);

            result.Items.Should().ContainSingle();
            result.Items[0].JobTitle.Should().Be("Senior Developer");
        }

        [Fact]
        public async Task Handle_ShouldSortBySalaryDescending_WhenRequested()
        {
            var enterpriseId = Guid.NewGuid();
            const int departmentId = 1;

            _context.Departments.Add(new Department { Id = departmentId, DepartmentName = "IT" });
            _context.Enterprises.Add(new Enterprise
            {
                Id = enterpriseId,
                EnterpriseName = "Sort Corp",
                EnterpriseCode = "SC1",
                Status = "Active",
                SubscriptionPlanId = Guid.NewGuid()
            });

            _context.JobPostings.AddRange(
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Mid Engineer",
                    Status = JobPostingStatus.Published,
                    DepartmentId = departmentId,
                    Description = "Code",
                    SalaryRangeMin = 15_000_000,
                    SalaryRangeMax = 20_000_000,
                    ShowSalary = true,
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow.AddDays(-1)
                },
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Lead Engineer",
                    Status = JobPostingStatus.Published,
                    DepartmentId = departmentId,
                    Description = "Lead",
                    SalaryRangeMin = 25_000_000,
                    SalaryRangeMax = 35_000_000,
                    ShowSalary = true,
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow.AddDays(-2)
                });
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new GetPublicJobPostingsQuery
            {
                SortBy = "salary_desc",
                PageNumber = 1,
                PageSize = 10
            }, CancellationToken.None);

            result.Items.Select(item => item.JobTitle).Should().ContainInOrder("Lead Engineer", "Mid Engineer");
        }

        [Fact]
        public async Task Handle_ShouldNotTreatThreeToFiveYearsAsFivePlus()
        {
            var enterpriseId = Guid.NewGuid();
            const int departmentId = 1;

            _context.Departments.Add(new Department { Id = departmentId, DepartmentName = "Engineering" });
            _context.Enterprises.Add(new Enterprise
            {
                Id = enterpriseId,
                EnterpriseName = "Experience Corp",
                EnterpriseCode = "EC1",
                Status = "Active",
                SubscriptionPlanId = Guid.NewGuid()
            });

            _context.JobPostings.AddRange(
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Senior Developer",
                    Status = JobPostingStatus.Published,
                    DepartmentId = departmentId,
                    Description = "Senior role",
                    ExperienceLevel = "3-5 years",
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow
                },
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Principal Developer",
                    Status = JobPostingStatus.Published,
                    DepartmentId = departmentId,
                    Description = "Principal role",
                    ExperienceLevel = "5-7 years",
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow.AddMinutes(-5)
                });
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new GetPublicJobPostingsQuery
            {
                ExperienceBucket = "5+",
                PageNumber = 1,
                PageSize = 10
            }, CancellationToken.None);

            result.Items.Should().ContainSingle();
            result.Items[0].JobTitle.Should().Be("Principal Developer");
        }

        [Fact]
        public async Task Handle_ShouldFallbackToNewest_WhenRelevanceRequestedWithoutSearch()
        {
            var enterpriseId = Guid.NewGuid();
            const int departmentId = 1;

            _context.Departments.Add(new Department { Id = departmentId, DepartmentName = "IT" });
            _context.Enterprises.Add(new Enterprise
            {
                Id = enterpriseId,
                EnterpriseName = "Relevant Corp",
                EnterpriseCode = "RC1",
                Status = "Active",
                SubscriptionPlanId = Guid.NewGuid()
            });

            _context.JobPostings.AddRange(
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Older Job",
                    Status = JobPostingStatus.Published,
                    DepartmentId = departmentId,
                    Description = "Old",
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow.AddDays(-3)
                },
                new JobPosting
                {
                    Id = Guid.NewGuid(),
                    EnterpriseId = enterpriseId,
                    JobTitle = "Newest Job",
                    Status = JobPostingStatus.Published,
                    DepartmentId = departmentId,
                    Description = "New",
                    CreatedById = Guid.NewGuid(),
                    PublishedAt = DateTime.UtcNow
                });
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new GetPublicJobPostingsQuery
            {
                SortBy = "relevance",
                PageNumber = 1,
                PageSize = 10
            }, CancellationToken.None);

            result.Items.First().JobTitle.Should().Be("Newest Job");
        }
    }
}
