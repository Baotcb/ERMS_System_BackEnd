using ERMS.Application.Features.JobPostings.Queries.GetPublicJobFilterOptions;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERMS.UnitTests.Features.JobPostings.Queries.GetPublicJobFilterOptions;

public sealed class GetPublicJobFilterOptionsHandlerTests
{
    private readonly ERMSDbContext _context;
    private readonly GetPublicJobFilterOptionsHandler _handler;

    public GetPublicJobFilterOptionsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ERMSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ERMSDbContext(options);
        _handler = new GetPublicJobFilterOptionsHandler(_context);
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyPublishedDepartmentsAndLocations()
    {
        var enterpriseId = Guid.NewGuid();
        _context.Departments.AddRange(
            new Department { Id = 1, DepartmentName = "Engineering" },
            new Department { Id = 2, DepartmentName = "Sales" });
        _context.Enterprises.Add(new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Meta Corp",
            EnterpriseCode = "MC1",
            Status = "Active",
            SubscriptionPlanId = Guid.NewGuid()
        });

        _context.JobPostings.AddRange(
            new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = 1,
                JobTitle = "Engineer",
                Description = "Build",
                Status = JobPostingStatus.Published,
                Location = "Hà Nội",
                CreatedById = Guid.NewGuid()
            },
            new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = 1,
                JobTitle = "Engineer 2",
                Description = "Build more",
                Status = JobPostingStatus.Published,
                Location = "Hồ Chí Minh",
                CreatedById = Guid.NewGuid()
            },
            new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = 2,
                JobTitle = "Draft Sales",
                Description = "Sell",
                Status = JobPostingStatus.Draft,
                Location = "Đà Nẵng",
                CreatedById = Guid.NewGuid()
            });

        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new GetPublicJobFilterOptionsQuery(), CancellationToken.None);

        result.Departments.Should().ContainSingle();
        result.Departments[0].DepartmentName.Should().Be("Engineering");
        result.Departments[0].JobCount.Should().Be(2);
        result.Locations.Should().BeEquivalentTo(["Hà Nội", "Hồ Chí Minh"]);
        result.EmploymentTypes.Should().NotBeEmpty();
        result.ExperienceBuckets.Should().NotBeEmpty();
        result.SalaryBuckets.Should().NotBeEmpty();
    }
}
