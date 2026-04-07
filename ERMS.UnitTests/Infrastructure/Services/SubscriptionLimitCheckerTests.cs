using ERMS.Application.Interface;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using ERMS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ERMS.UnitTests.Infrastructure.Services;

public class SubscriptionLimitCheckerTests : IDisposable
{
    private readonly ERMSDbContext _context;
    private readonly ISubscriptionLimitChecker _checker;

    public SubscriptionLimitCheckerTests()
    {
        var options = new DbContextOptionsBuilder<ERMSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ERMSDbContext(options);
        _checker = new SubscriptionLimitChecker(_context);
    }

    [Fact]
    public async Task CheckJobPostingLimitAsync_ShouldReturnAllowed_WhenUnderLimit()
    {
        // Arrange
        var (enterpriseId, _) = await SeedEnterpriseWithPlanAsync("FREE", maxJobPostings: 2, maxCourses: 2);
        await SeedJobPostingsAsync(enterpriseId, count: 1);

        // Act
        var result = await _checker.CheckJobPostingLimitAsync(enterpriseId, CancellationToken.None);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.CurrentCount.Should().Be(1);
        result.MaxAllowed.Should().Be(2);
    }

    [Fact]
    public async Task CheckJobPostingLimitAsync_ShouldReturnNotAllowed_WhenAtLimit()
    {
        // Arrange
        var (enterpriseId, _) = await SeedEnterpriseWithPlanAsync("FREE", maxJobPostings: 2, maxCourses: 2);
        await SeedJobPostingsAsync(enterpriseId, count: 2);

        // Act
        var result = await _checker.CheckJobPostingLimitAsync(enterpriseId, CancellationToken.None);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.CurrentCount.Should().Be(2);
        result.MaxAllowed.Should().Be(2);
        result.Message.Should().Contain("Đã đạt giới hạn");
    }

    [Fact]
    public async Task CheckCourseLimitAsync_ShouldReturnNotAllowed_WhenAtLimit()
    {
        // Arrange
        var (enterpriseId, _) = await SeedEnterpriseWithPlanAsync("FREE", maxJobPostings: 2, maxCourses: 2);
        await SeedCoursesAsync(enterpriseId, count: 2);

        // Act
        var result = await _checker.CheckCourseLimitAsync(enterpriseId, CancellationToken.None);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.CurrentCount.Should().Be(2);
        result.MaxAllowed.Should().Be(2);
    }

    [Theory]
    [InlineData("FREE", false)]
    [InlineData("PRO", true)]
    public async Task IsProPlanAsync_ShouldUsePlanCode(string planCode, bool expected)
    {
        // Arrange
        var (enterpriseId, _) = await SeedEnterpriseWithPlanAsync(planCode, maxJobPostings: 2, maxCourses: 2);

        // Act
        var result = await _checker.IsProPlanAsync(enterpriseId, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
    }

    private async Task<(Guid enterpriseId, Guid planId)> SeedEnterpriseWithPlanAsync(string planCode, int maxJobPostings, int maxCourses)
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanName = $"{planCode} Plan",
            PlanCode = planCode,
            IsActive = true,
            IsDeleted = false,
            MaxJobPostings = maxJobPostings,
            MaxCourses = maxCourses
        };
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            EnterpriseName = "Acme",
            EnterpriseCode = "ACME",
            SubscriptionPlanId = plan.Id,
            SubscriptionPlan = plan,
            SubscriptionStartDate = DateTime.UtcNow.AddDays(-5),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(85),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _context.SubscriptionPlans.Add(plan);
        _context.Enterprises.Add(enterprise);
        await _context.SaveChangesAsync();
        return (enterprise.Id, plan.Id);
    }

    private async Task SeedJobPostingsAsync(Guid enterpriseId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _context.JobPostings.Add(new JobPosting
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DepartmentId = 1,
                JobTitle = $"Job {i}",
                Description = "desc",
                CreatedById = Guid.NewGuid(),
                Status = "Published",
                IsDeleted = false
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedCoursesAsync(Guid enterpriseId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _context.Courses.Add(new Course
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                CourseName = $"Course {i}",
                CourseCode = $"C{i}",
                TrainerEmail = "trainer@acme.vn",
                Status = "Published",
                IsDeleted = false
            });
        }

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
