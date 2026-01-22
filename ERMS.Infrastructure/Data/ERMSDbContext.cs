using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ERMS.Domain.Entities.Application;

namespace ERMS.Infrastructure.Data
{
    public class ERMSDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IERMSDbContext
    {
        public ERMSDbContext(DbContextOptions<ERMSDbContext> options) : base(options)
        {
        }

        // Core Platform Entities
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Enterprise> Enterprises { get; set; }
        public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        
        // Organization Entities
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        
        // Recruitment Flow Entities
        public DbSet<RecruitmentPlan> RecruitmentPlans { get; set; }
        public DbSet<PlanDetail> PlanDetails { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<ApplicationEntity> Applications { get; set; }
        public DbSet<CVScreeningResult> CVScreeningResults { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<InterviewParticipant> InterviewParticipants { get; set; }
        public DbSet<Offer> Offers { get; set; }
        
        // Candidate Profile Entities
        public DbSet<Skill> Skills { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<CandidateSkill> CandidateSkills { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        
        // System Entities
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added && entry.Entity.GetType().GetProperty("CreatedAt") != null)
                {
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified && entry.Entity.GetType().GetProperty("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =============================================
            // IDENTITY TABLES NAMING
            // =============================================
            builder.Entity<User>().ToTable("Users");
            builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

            // =============================================
            // PLATFORM ENTITIES
            // =============================================
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasIndex(sp => sp.Code).IsUnique();
                entity.Property(sp => sp.Price).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Enterprise>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CurrentSubscriptionId);

                entity.HasOne(e => e.CurrentSubscription)
                    .WithMany()
                    .HasForeignKey(e => e.CurrentSubscriptionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<SubscriptionHistory>(entity =>
            {
                entity.HasIndex(sh => sh.EnterpriseId);
                entity.HasIndex(sh => sh.PaymentStatus);
                
                entity.Property(sh => sh.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(sh => sh.Enterprise)
                    .WithMany(e => e.SubscriptionHistories)
                    .HasForeignKey(sh => sh.EnterpriseId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(sh => sh.SubscriptionPlan)
                    .WithMany(sp => sp.SubscriptionHistories)
                    .HasForeignKey(sh => sh.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Restrict -> NoAction
            });

            // =============================================
            // USER & ORGANIZATION - SỬA TOÀN BỘ CASCADE
            // =============================================
            builder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.EnterpriseId);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.DepartmentId);

                entity.HasOne(u => u.Enterprise)
                    .WithMany(e => e.Users)
                    .HasForeignKey(u => u.EnterpriseId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                // ✅ THAY ĐỔI QUAN TRỌNG: Cascade -> NoAction
                entity.HasOne(u => u.Employee)
                    .WithOne(e => e.User)
                    .HasForeignKey<Employee>(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // ❌ XÓA CASCADE

                entity.HasOne(u => u.Candidate)
                    .WithOne(c => c.User)
                    .HasForeignKey<Candidate>(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // ❌ XÓA CASCADE
            });

            builder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.HasIndex(d => d.EnterpriseId);
                entity.HasIndex(d => d.ManagerId);
                entity.HasIndex(d => d.ParentDepartmentId);
                entity.HasIndex(d => new { d.EnterpriseId, d.DepartmentCode }).IsUnique();

                entity.HasOne(d => d.Enterprise)
                    .WithMany(e => e.Departments)
                    .HasForeignKey(d => d.EnterpriseId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Manager)
                    .WithMany()
                    .HasForeignKey(d => d.ManagerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.ParentDepartment)
                    .WithMany(parent => parent.SubDepartments)
                    .HasForeignKey(d => d.ParentDepartmentId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Restrict -> NoAction
            });

            builder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.DepartmentId);
                entity.HasIndex(e => e.EmployeeCode).IsUnique();

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Candidate>(entity =>
            {
                entity.HasKey(c => c.UserId);
                entity.Property(c => c.ExpectedSalary).HasColumnType("decimal(18,2)");
            });

            // =============================================
            // RECRUITMENT FLOW
            // =============================================
            builder.Entity<RecruitmentPlan>(entity =>
            {
                entity.HasIndex(rp => rp.EnterpriseId);
                entity.HasIndex(rp => rp.Status);
                entity.HasIndex(rp => rp.CreatedById);
                
                entity.Property(rp => rp.TotalBudget).HasColumnType("decimal(18,2)");

                entity.HasOne(rp => rp.Enterprise)
                    .WithMany(e => e.RecruitmentPlans)
                    .HasForeignKey(rp => rp.EnterpriseId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(rp => rp.CreatedBy)
                    .WithMany()
                    .HasForeignKey(rp => rp.CreatedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(rp => rp.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(rp => rp.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<PlanDetail>(entity =>
            {
                entity.HasIndex(pd => pd.RecruitmentPlanId);
                entity.HasIndex(pd => pd.DepartmentId);
                entity.HasIndex(pd => pd.RequestedById);
                entity.HasIndex(pd => pd.Status);
                
                entity.Property(pd => pd.MinSalary).HasColumnType("decimal(18,2)");
                entity.Property(pd => pd.MaxSalary).HasColumnType("decimal(18,2)");

                entity.HasOne(pd => pd.RecruitmentPlan)
                    .WithMany(rp => rp.PlanDetails)
                    .HasForeignKey(pd => pd.RecruitmentPlanId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN - Clear parent-child

                entity.HasOne(pd => pd.Department)
                    .WithMany(d => d.PlanDetails)
                    .HasForeignKey(pd => pd.DepartmentId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(pd => pd.RequestedBy)
                    .WithMany()
                    .HasForeignKey(pd => pd.RequestedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(pd => pd.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(pd => pd.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ApprovalHistory>(entity =>
            {
                entity.HasIndex(ah => new { ah.EntityType, ah.EntityId });
                entity.HasIndex(ah => ah.ApproverId);

                entity.HasOne(ah => ah.Approver)
                    .WithMany()
                    .HasForeignKey(ah => ah.ApproverId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<JobPosting>(entity =>
            {
                entity.HasIndex(jp => jp.EnterpriseId);
                entity.HasIndex(jp => jp.PlanDetailId);
                entity.HasIndex(jp => jp.Status);
                entity.HasIndex(jp => jp.Slug).IsUnique();
                entity.HasIndex(jp => jp.PublishedAt);
                
                entity.Property(jp => jp.MinSalary).HasColumnType("decimal(18,2)");
                entity.Property(jp => jp.MaxSalary).HasColumnType("decimal(18,2)");

                entity.HasOne(jp => jp.Enterprise)
                    .WithMany(e => e.JobPostings)
                    .HasForeignKey(jp => jp.EnterpriseId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(jp => jp.PlanDetail)
                    .WithOne(pd => pd.JobPosting)
                    .HasForeignKey<JobPosting>(jp => jp.PlanDetailId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(jp => jp.Department)
                    .WithMany(d => d.JobPostings)
                    .HasForeignKey(jp => jp.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(jp => jp.CreatedBy)
                    .WithMany()
                    .HasForeignKey(jp => jp.CreatedById)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<ApplicationEntity>(entity =>
            {
                entity.HasIndex(a => a.JobPostingId);
                entity.HasIndex(a => a.CandidateId);
                entity.HasIndex(a => a.Stage);
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => new { a.JobPostingId, a.CandidateId }).IsUnique();

                entity.HasOne(a => a.JobPosting)
                    .WithMany(jp => jp.Applications)
                    .HasForeignKey(a => a.JobPostingId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(a => a.Candidate)
                    .WithMany(c => c.Applications)
                    .HasForeignKey(a => a.CandidateId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(a => a.Resume)
                    .WithMany(r => r.Applications)
                    .HasForeignKey(a => a.ResumeId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.ReferredBy)
                    .WithMany()
                    .HasForeignKey(a => a.ReferredById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<CVScreeningResult>(entity =>
            {
                entity.HasIndex(csr => csr.ApplicationId).IsUnique();
                entity.HasIndex(csr => csr.OverallScore);
                entity.HasIndex(csr => csr.Recommendation);
                
                entity.Property(csr => csr.OverallScore).HasColumnType("decimal(5,2)");
                entity.Property(csr => csr.SkillMatchScore).HasColumnType("decimal(5,2)");
                entity.Property(csr => csr.ExperienceMatchScore).HasColumnType("decimal(5,2)");
                entity.Property(csr => csr.EducationMatchScore).HasColumnType("decimal(5,2)");

                entity.HasOne(csr => csr.Application)
                    .WithOne(a => a.CVScreeningResult)
                    .HasForeignKey<CVScreeningResult>(csr => csr.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN - One-to-one dependency
            });

            builder.Entity<Interview>(entity =>
            {
                entity.HasIndex(i => i.ApplicationId);
                entity.HasIndex(i => i.ScheduledAt);
                entity.HasIndex(i => i.Status);

                entity.HasOne(i => i.Application)
                    .WithMany(a => a.Interviews)
                    .HasForeignKey(i => i.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN - Clear parent-child

                entity.HasOne(i => i.SelectedBy)
                    .WithMany()
                    .HasForeignKey(i => i.SelectedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(i => i.CreatedBy)
                    .WithMany()
                    .HasForeignKey(i => i.CreatedById)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<InterviewParticipant>(entity =>
            {
                entity.HasIndex(ip => ip.InterviewId);
                entity.HasIndex(ip => ip.UserId);
                entity.HasIndex(ip => new { ip.InterviewId, ip.UserId }).IsUnique();

                entity.HasOne(ip => ip.Interview)
                    .WithMany(i => i.Participants)
                    .HasForeignKey(ip => ip.InterviewId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN - Clear parent-child

                entity.HasOne(ip => ip.User)
                    .WithMany()
                    .HasForeignKey(ip => ip.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Restrict -> NoAction
            });

            builder.Entity<Offer>(entity =>
            {
                entity.HasIndex(o => o.ApplicationId).IsUnique();
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.ExpiresAt);
                
                entity.Property(o => o.OfferedSalary).HasColumnType("decimal(18,2)");

                entity.HasOne(o => o.Application)
                    .WithOne(a => a.Offer)
                    .HasForeignKey<Offer>(o => o.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN - One-to-one dependency

                entity.HasOne(o => o.Department)
                    .WithMany(d => d.Offers)
                    .HasForeignKey(o => o.DepartmentId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Restrict -> NoAction

                entity.HasOne(o => o.CreatedBy)
                    .WithMany()
                    .HasForeignKey(o => o.CreatedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(o => o.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(o => o.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // =============================================
            // CANDIDATE PROFILE - SỬA CASCADE BEHAVIORS
            // =============================================
            builder.Entity<Skill>(entity =>
            {
                entity.HasIndex(s => s.Name).IsUnique();
                entity.HasIndex(s => s.Category);
            });

            builder.Entity<JobSkill>(entity =>
            {
                entity.HasIndex(js => js.JobPostingId);
                entity.HasIndex(js => js.SkillId);
                entity.HasIndex(js => new { js.JobPostingId, js.SkillId }).IsUnique();

                entity.HasOne(js => js.JobPosting)
                    .WithMany(jp => jp.JobSkills)
                    .HasForeignKey(js => js.JobPostingId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN - Clear many-to-many junction

                entity.HasOne(js => js.Skill)
                    .WithMany(s => s.JobSkills)
                    .HasForeignKey(js => js.SkillId)
                    .OnDelete(DeleteBehavior.Cascade); // ✅ GIỮ NGUYÊN
            });

            builder.Entity<CandidateSkill>(entity =>
            {
                entity.HasIndex(cs => cs.CandidateId);
                entity.HasIndex(cs => cs.SkillId);
                entity.HasIndex(cs => new { cs.CandidateId, cs.SkillId }).IsUnique();

                entity.HasOne(cs => cs.Candidate)
                    .WithMany(c => c.CandidateSkills)
                    .HasForeignKey(cs => cs.CandidateId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction

                entity.HasOne(cs => cs.Skill)
                    .WithMany(s => s.CandidateSkills)
                    .HasForeignKey(cs => cs.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Education>(entity =>
            {
                entity.HasIndex(e => e.CandidateId);

                entity.HasOne(e => e.Candidate)
                    .WithMany(c => c.Educations)
                    .HasForeignKey(e => e.CandidateId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction
            });

            builder.Entity<WorkExperience>(entity =>
            {
                entity.HasIndex(we => we.CandidateId);

                entity.HasOne(we => we.Candidate)
                    .WithMany(c => c.WorkExperiences)
                    .HasForeignKey(we => we.CandidateId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction
            });

            builder.Entity<Resume>(entity =>
            {
                entity.HasIndex(r => r.CandidateId);
                entity.HasIndex(r => r.IsPrimary);

                entity.HasOne(r => r.Candidate)
                    .WithMany(c => c.Resumes)
                    .HasForeignKey(r => r.CandidateId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction
            });

            // =============================================
            // SYSTEM
            // =============================================
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.IsRead);
                entity.HasIndex(n => n.CreatedAt);

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction
            });

            builder.Entity<SavedJob>(entity =>
            {
                entity.HasIndex(sj => sj.CandidateId);
                entity.HasIndex(sj => sj.JobPostingId);
                entity.HasIndex(sj => new { sj.CandidateId, sj.JobPostingId }).IsUnique();

                entity.HasOne(sj => sj.Candidate)
                    .WithMany(c => c.SavedJobs)
                    .HasForeignKey(sj => sj.CandidateId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction

                entity.HasOne(sj => sj.JobPosting)
                    .WithMany(jp => jp.SavedJobs)
                    .HasForeignKey(sj => sj.JobPostingId)
                    .OnDelete(DeleteBehavior.NoAction); // ✅ THAY ĐỔI: Cascade -> NoAction
            });
        }
    }
}
