using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Application.Features.Admin.Queries.GetEnterpriseAdminDetail;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Enterprise;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERMS.UnitTests.Features.Admin.Queries.GetEnterpriseAdminDetail;

public class GetEnterpriseAdminDetailHandlerTests
{
    private readonly Mock<IERMSDbContext> _mockContext;
    private readonly GetEnterpriseAdminDetailHandler _handler;

    public GetEnterpriseAdminDetailHandlerTests()
    {
        _mockContext = new Mock<IERMSDbContext>();
        _handler = new GetEnterpriseAdminDetailHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnEnterpriseDetail_WhenEnterpriseExists()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Tech Corp",
            EnterpriseCode = "TC001",
            TaxCode = "123456789",
            Address = "123 Tech Street",
            Phone = "0123456789",
            Email = "contact@techcorp.vn",
            Website = "https://techcorp.vn",
            LogoUrl = "https://cdn.example.com/logo.png",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan
            {
                Id = planId,
                PlanName = "Pro",
                PlanCode = "PRO",
                MaxUsers = 100,
                MaxJobPostings = 25,
                MaxCourses = 20,
                PriceMonthly = 1500000,
                PriceYearly = 15000000
            },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(12),
            SubscriptionStatus = "Active",
            CreatedById = adminId,
            CreatedBy = new User
            {
                Id = adminId,
                FullName = "Nguyen Van Admin"
            },
            IsDeleted = false
        };

        var departments = new List<Department>
        {
            new() { Id = 1, EnterpriseId = enterpriseId, DepartmentName = "HR", IsDeleted = false },
            new() { Id = 2, EnterpriseId = enterpriseId, DepartmentName = "IT", IsDeleted = false },
            new() { Id = 3, EnterpriseId = Guid.NewGuid(), DepartmentName = "Other", IsDeleted = false }
        };

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "EMP001", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "EMP002", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "EMP003", UserId = Guid.NewGuid(), IsDeleted = true }
        };

        var payments = new List<SubscriptionHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                SubscriptionPlanId = planId,
                SubscriptionPlan = enterprise.SubscriptionPlan,
                Amount = 1500000,
                Currency = "VND",
                PaymentReference = "PAY-001",
                PaymentMethod = "BankTransfer",
                PeriodStartDate = new DateTime(2026, 1, 1),
                PeriodEndDate = new DateTime(2026, 12, 31),
                CreatedAt = new DateTime(2026, 2, 1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                SubscriptionPlanId = planId,
                SubscriptionPlan = enterprise.SubscriptionPlan,
                Amount = 1200000,
                Currency = "VND",
                PaymentMethod = "Cash",
                PeriodStartDate = new DateTime(2025, 1, 1),
                PeriodEndDate = new DateTime(2025, 12, 31),
                CreatedAt = new DateTime(2025, 2, 1)
            }
        };

        var jobPostings = new List<JobPosting>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DepartmentId = 1, JobTitle = "Backend", Description = "desc", CreatedById = adminId, IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DepartmentId = 1, JobTitle = "Frontend", Description = "desc", CreatedById = adminId, IsDeleted = false }
        };

        var courses = new List<Course>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, CourseName = "Onboarding", CourseCode = "TR001", TrainerEmail = "trainer@test.vn", StartTime = DateTime.UtcNow, IsDeleted = false }
        };

        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseId,
                Action = "StatusChange",
                PreviousStatus = EnterpriseStatus.Suspended,
                NewStatus = EnterpriseStatus.Active,
                PerformedById = actorId,
                PerformedBy = new User { Id = actorId, FullName = "Admin Tran" },
                Note = "Da xac minh va mo lai",
                CreatedAt = new DateTime(2026, 3, 20, 14, 30, 0, DateTimeKind.Utc)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(departments.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(employees.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(payments.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(jobPostings.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(courses.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.EnterpriseId.Should().Be(enterpriseId);
        result.EnterpriseName.Should().Be("Tech Corp");
        result.EnterpriseCode.Should().Be("TC001");
        result.TaxCode.Should().Be("123456789");
        result.Address.Should().Be("123 Tech Street");
        result.Phone.Should().Be("0123456789");
        result.Email.Should().Be("contact@techcorp.vn");
        result.Website.Should().Be("https://techcorp.vn");
        result.LogoUrl.Should().Be("https://cdn.example.com/logo.png");
        result.Status.Should().Be(EnterpriseStatus.Active);
        result.CreatedByName.Should().Be("Nguyen Van Admin");
        result.CurrentPlan.PlanName.Should().Be("Pro");
        result.CurrentPlan.PlanCode.Should().Be("PRO");
        result.SubscriptionStartDate.Should().Be(new DateTime(2026, 1, 1));
        result.SubscriptionStatus.Should().Be("Active");
        result.DepartmentCount.Should().Be(2);
        result.EmployeeCount.Should().Be(2);
        result.JobPostingCount.Should().Be(2);
        result.CourseCount.Should().Be(1);
        result.TotalSpent.Should().Be(2700000);
        result.RecentPayment.Should().NotBeNull();
        result.RecentPayment!.Amount.Should().Be(1500000);
        result.RecentPayment.PaymentMethod.Should().Be("BankTransfer");
        result.RecentPayment.PaidAt.Should().Be(new DateTime(2026, 2, 1));
        result.StatusHistory.Should().ContainSingle();
        result.StatusHistory[0].ChangedByName.Should().Be("Admin Tran");
        result.RiskFlags.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseDoesNotExist()
    {
        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise>().AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenEnterpriseIsSoftDeleted()
    {
        var enterpriseId = Guid.NewGuid();
        var deletedEnterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Deleted Corp",
            EnterpriseCode = "DEL001",
            Status = EnterpriseStatus.Locked,
            IsDeleted = true
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { deletedEnterprise }.AsQueryable().BuildMockDbSet().Object);

        var act = async () => await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyChangedByName_WhenStatusHistoryActorIsMissing()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Tech Corp",
            EnterpriseCode = "TC001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan
            {
                Id = planId,
                PlanName = "Pro",
                PlanCode = "PRO"
            },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(15),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseId,
                Action = "StatusChange",
                PreviousStatus = EnterpriseStatus.Suspended,
                NewStatus = EnterpriseStatus.Active,
                PerformedById = Guid.NewGuid(),
                PerformedBy = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.StatusHistory.Should().ContainSingle();
        result.StatusHistory[0].ChangedByName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldFlagMissingPaymentHistory_WhenEnterpriseHasNoPayments()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "No Payment Corp",
            EnterpriseCode = "NP001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan
            {
                Id = planId,
                PlanName = "Basic",
                PlanCode = "FREE"
            },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(10),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.RecentPayment.Should().BeNull();
        result.TotalSpent.Should().Be(0);
        result.RiskFlags.Should().Contain(flag => flag.Contains("Chua co lich su thanh toan"));
    }

    [Fact]
    public async Task Handle_ShouldOrderStatusHistoryByNewestFirst()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "History Corp",
            EnterpriseCode = "HS001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseId,
                Action = "Suspend",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Suspended,
                CreatedAt = new DateTime(2026, 3, 1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseId,
                Action = "Activate",
                PreviousStatus = EnterpriseStatus.Suspended,
                NewStatus = EnterpriseStatus.Active,
                CreatedAt = new DateTime(2026, 3, 5)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.StatusHistory.Should().HaveCount(2);
        result.StatusHistory[0].Action.Should().Be("Activate");
        result.StatusHistory[1].Action.Should().Be("Suspend");
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyNonDeletedUsageRecords_ForTargetEnterprise()
    {
        var enterpriseId = Guid.NewGuid();
        var otherEnterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Usage Corp",
            EnterpriseCode = "US001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>
        {
            new() { Id = 1, EnterpriseId = enterpriseId, DepartmentName = "HR", IsDeleted = false },
            new() { Id = 2, EnterpriseId = enterpriseId, DepartmentName = "IT", IsDeleted = true },
            new() { Id = 3, EnterpriseId = otherEnterpriseId, DepartmentName = "Other", IsDeleted = false }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "E1", UserId = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, EmployeeCode = "E2", UserId = Guid.NewGuid(), IsDeleted = true },
            new() { Id = Guid.NewGuid(), EnterpriseId = otherEnterpriseId, EmployeeCode = "E3", UserId = Guid.NewGuid(), IsDeleted = false }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DepartmentId = 1, JobTitle = "Backend", Description = "desc", CreatedById = Guid.NewGuid(), IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DepartmentId = 1, JobTitle = "Frontend", Description = "desc", CreatedById = Guid.NewGuid(), IsDeleted = true },
            new() { Id = Guid.NewGuid(), EnterpriseId = otherEnterpriseId, DepartmentId = 1, JobTitle = "Other", Description = "desc", CreatedById = Guid.NewGuid(), IsDeleted = false }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, CourseName = "Course A", CourseCode = "C1", TrainerEmail = "a@test.vn", StartTime = DateTime.UtcNow, IsDeleted = false },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, CourseName = "Course B", CourseCode = "C2", TrainerEmail = "b@test.vn", StartTime = DateTime.UtcNow, IsDeleted = true },
            new() { Id = Guid.NewGuid(), EnterpriseId = otherEnterpriseId, CourseName = "Course C", CourseCode = "C3", TrainerEmail = "c@test.vn", StartTime = DateTime.UtcNow, IsDeleted = false }
        }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.DepartmentCount.Should().Be(1);
        result.EmployeeCount.Should().Be(1);
        result.JobPostingCount.Should().Be(1);
        result.CourseCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldPickNewestPaymentAsRecentPayment()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Payment Corp",
            EnterpriseCode = "PC001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        var payments = new List<SubscriptionHistory>
        {
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, SubscriptionPlanId = planId, SubscriptionPlan = enterprise.SubscriptionPlan, Amount = 1000000, PaymentMethod = "Cash", CreatedAt = new DateTime(2026, 1, 1) },
            new() { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, SubscriptionPlanId = planId, SubscriptionPlan = enterprise.SubscriptionPlan, Amount = 2000000, PaymentMethod = "BankTransfer", CreatedAt = new DateTime(2026, 2, 1) }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(payments.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.RecentPayment.Should().NotBeNull();
        result.RecentPayment!.Amount.Should().Be(2000000);
        result.RecentPayment.PaymentMethod.Should().Be("BankTransfer");
    }

    [Fact]
    public async Task Handle_ShouldAddLockedAndExpiredRiskFlags()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Locked Corp",
            EnterpriseCode = "LK001",
            Status = EnterpriseStatus.Locked,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(-3),
            SubscriptionStatus = "Expired",
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.RiskFlags.Should().Contain(flag => flag.Contains("bi khoa"));
        result.RiskFlags.Should().Contain(flag => flag.Contains("het han"));
        result.RiskFlags.Should().Contain(flag => flag.Contains("Chua co lich su thanh toan"));
    }

    [Fact]
    public async Task Handle_ShouldReturnNullCreatedByName_WhenCreatorIsMissing()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Creatorless Corp",
            EnterpriseCode = "CR001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Free", PlanCode = "FREE" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(60),
            SubscriptionStatus = "Active",
            CreatedBy = null,
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.CreatedByName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldIgnoreNonEnterpriseApprovalHistory_WhenBuildingStatusHistory()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "History Filter Corp",
            EnterpriseCode = "HF001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(45),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        var approvalHistories = new List<ApprovalHistory>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "Enterprise",
                EntityId = enterpriseId,
                Action = "Suspend",
                PreviousStatus = EnterpriseStatus.Active,
                NewStatus = EnterpriseStatus.Suspended,
                CreatedAt = new DateTime(2026, 3, 5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                EntityType = "JobPosting",
                EntityId = enterpriseId,
                Action = "Archive",
                CreatedAt = new DateTime(2026, 3, 6)
            }
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(approvalHistories.AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.StatusHistory.Should().ContainSingle();
        result.StatusHistory[0].Action.Should().Be("Suspend");
    }

    [Fact]
    public async Task Handle_ShouldIncludeRemainingDays_WhenSubscriptionExpiresWithinThirtyDays()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Expiring Detail Corp",
            EnterpriseCode = "ED001",
            Status = EnterpriseStatus.Active,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = now.AddDays(7),
            SubscriptionStatus = "Active",
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.RiskFlags.Should().Contain(flag => flag.Contains("Subscription sap het han trong"));
    }

    [Fact]
    public async Task Handle_ShouldAddSuspendedRiskFlag_WhenEnterpriseStatusIsSuspended()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Suspended Detail Corp",
            EnterpriseCode = "SD001",
            Status = EnterpriseStatus.Suspended,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Pro", PlanCode = "PRO" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(60),
            SubscriptionStatus = "Suspended",
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.RiskFlags.Should().Contain(flag => flag.Contains("tam dung"));
    }

    [Fact]
    public async Task Handle_ShouldAddInactiveRiskFlag_WhenEnterpriseStatusIsInactive()
    {
        var enterpriseId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var enterprise = new Enterprise
        {
            Id = enterpriseId,
            EnterpriseName = "Inactive Detail Corp",
            EnterpriseCode = "ID001",
            Status = EnterpriseStatus.Inactive,
            SubscriptionPlanId = planId,
            SubscriptionPlan = new SubscriptionPlan { Id = planId, PlanName = "Free", PlanCode = "FREE" },
            SubscriptionStartDate = new DateTime(2026, 1, 1),
            SubscriptionEndDate = DateTime.UtcNow.AddDays(60),
            SubscriptionStatus = "Inactive",
            IsDeleted = false
        };

        _mockContext.Setup(x => x.Enterprises).Returns(new List<Enterprise> { enterprise }.AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Departments).Returns(new List<Department>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Employees).Returns(new List<Employee>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.SubscriptionHistories).Returns(new List<SubscriptionHistory>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.JobPostings).Returns(new List<JobPosting>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.Courses).Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);
        _mockContext.Setup(x => x.ApprovalHistories).Returns(new List<ApprovalHistory>().AsQueryable().BuildMockDbSet().Object);

        var result = await _handler.Handle(new GetEnterpriseAdminDetailQuery { EnterpriseId = enterpriseId }, CancellationToken.None);

        result.RiskFlags.Should().Contain(flag => flag.Contains("ngung hoat dong"));
    }
}
