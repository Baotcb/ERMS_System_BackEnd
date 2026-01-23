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

        // DbSets
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Enterprise> Enterprises { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<CandidateSkill> CandidateSkills { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<ApplicationEntity> Applications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSkill> CourseSkills { get; set; }
        public DbSet<CourseSession> CourseSessions { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<EnrollmentRequest> EnrollmentRequests { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<CourseFeedback> CourseFeedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
           
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
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
            // ENTERPRISE & SUBSCRIPTION PLAN
            // =============================================
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(sp => sp.Id);
                entity.HasIndex(sp => sp.PlanCode).IsUnique();
                entity.Property(sp => sp.PriceMonthly).HasColumnType("decimal(18,2)");
                entity.Property(sp => sp.PriceYearly).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Enterprise>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EnterpriseCode).IsUnique();

                entity.HasOne(e => e.SubscriptionPlan)
                    .WithMany(sp => sp.Enterprises)
                    .HasForeignKey(e => e.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.SubscriptionStatus)
                    .HasMaxLength(30);

                entity.ToTable(t => t.HasCheckConstraint("CK_Enterprise_SubscriptionStatus", 
                    "SubscriptionStatus IN ('Active', 'Expired', 'Cancelled', 'Trial', 'PastDue')"));
            });

            // =============================================
            // USER & DEPARTMENT
            // =============================================
            builder.Entity<User>(entity =>
            {
                //entity.HasOne(u => u.Enterprise)
                //    .WithMany(e => e.Users)
                //    .HasForeignKey(u => u.EnterpriseId)
                //    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Department)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.Employee)
                    .WithOne(e => e.User)
                    .HasForeignKey<Employee>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(u => u.Candidate)
                    .WithOne(c => c.User)
                    .HasForeignKey<Candidate>(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.HasOne(d => d.Enterprise)
                    .WithMany(e => e.Departments)
                    .HasForeignKey(d => d.EnterpriseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Manager)
                    .WithMany()
                    .HasForeignKey(d => d.ManagerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(d => d.Id)
                    .ValueGeneratedOnAdd();
            });

            // =============================================
            // EMPLOYEE & CANDIDATE
            // =============================================
            builder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.HasOne(e => e.Enterprise)
                    .WithMany(ent => ent.Employees)
                    .HasForeignKey(e => e.EnterpriseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.EnterpriseId, e.EmployeeCode }).IsUnique();
            });

            builder.Entity<Candidate>(entity =>
            {
                entity.HasKey(c => c.UserId);
            });

            // =============================================
            // SKILLS
            // =============================================
            builder.Entity<Skill>(entity =>
            {
                entity.HasOne(s => s.Enterprise)
                    .WithMany(e => e.Skills)
                    .HasForeignKey(s => s.EnterpriseId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(s => s.Name).IsUnique();
                
                entity.Property(s => s.Id)
                    .ValueGeneratedOnAdd();
            });

            // =============================================
            // JOB POSTING & APPLICATIONS
            // =============================================
            builder.Entity<JobPosting>(entity =>
            {
                entity.HasOne(j => j.Enterprise)
                    .WithMany(e => e.JobPostings)
                    .HasForeignKey(j => j.EnterpriseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(j => j.Department)
                    .WithMany(d => d.JobPostings)
                    .HasForeignKey(j => j.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(j => j.Creator)
                    .WithMany(e => e.CreatedJobPostings)
                    .HasForeignKey(j => j.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(j => j.MinSalary).HasColumnType("decimal(18,2)");
                entity.Property(j => j.MaxSalary).HasColumnType("decimal(18,2)");
            });

            builder.Entity<JobSkill>(entity =>
            {
                entity.HasKey(js => new { js.JobId, js.SkillId });

                entity.HasOne(js => js.Job)
                    .WithMany(j => j.JobSkills)
                    .HasForeignKey(js => js.JobId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(js => js.Skill)
                    .WithMany(s => s.JobSkills)
                    .HasForeignKey(js => js.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CandidateSkill>(entity =>
            {
                entity.HasKey(cs => new { cs.CandidateId, cs.SkillId });

                entity.HasOne(cs => cs.Candidate)
                    .WithMany(c => c.CandidateSkills)
                    .HasForeignKey(cs => cs.CandidateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Skill)
                    .WithMany(s => s.CandidateSkills)
                    .HasForeignKey(cs => cs.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============================================
            // RESUME & RELATED
            // =============================================
            builder.Entity<Resume>(entity =>
            {
                entity.HasOne(r => r.Candidate)
                    .WithMany(c => c.Resumes)
                    .HasForeignKey(r => r.CandidateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Education>(entity =>
            {
                entity.HasOne(e => e.Resume)
                    .WithMany(r => r.Educations)
                    .HasForeignKey(e => e.ResumeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
            });

            builder.Entity<WorkExperience>(entity =>
            {
                entity.HasOne(w => w.Resume)
                    .WithMany(r => r.WorkExperiences)
                    .HasForeignKey(w => w.ResumeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(w => w.Id)
                    .ValueGeneratedOnAdd();
            });

            // =============================================
            // APPLICATION & INTERVIEW & OFFER
            // =============================================
            builder.Entity<ApplicationEntity>(entity =>
            {
                entity.HasOne(a => a.Job)
                    .WithMany(j => j.Applications)
                    .HasForeignKey(a => a.JobId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Candidate)
                    .WithMany(c => c.Applications)
                    .HasForeignKey(a => a.CandidateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Resume)
                    .WithMany(r => r.Applications)
                    .HasForeignKey(a => a.ResumeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Interview>(entity =>
            {
                entity.HasOne(i => i.Application)
                    .WithMany(a => a.Interviews)
                    .HasForeignKey(i => i.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Interviewer)
                    .WithMany(e => e.Interviews)
                    .HasForeignKey(i => i.InterviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Offer>(entity =>
            {
                entity.HasOne(o => o.Application)
                    .WithOne(a => a.Offer)
                    .HasForeignKey<Offer>(o => o.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(o => o.Salary).HasColumnType("decimal(18,2)");
            });

            // =============================================
            // COURSE & TRAINING
            // =============================================
            builder.Entity<Course>(entity =>
            {
                entity.HasOne(c => c.Enterprise)
                    .WithMany(e => e.Courses)
                    .HasForeignKey(c => c.EnterpriseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Creator)
                    .WithMany(e => e.CreatedCourses)
                    .HasForeignKey(c => c.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CourseSkill>(entity =>
            {
                entity.HasKey(cs => new { cs.CourseId, cs.SkillId });

                entity.HasOne(cs => cs.Course)
                    .WithMany(c => c.CourseSkills)
                    .HasForeignKey(cs => cs.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Skill)
                    .WithMany(s => s.CourseSkills)
                    .HasForeignKey(cs => cs.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CourseSession>(entity =>
            {
                entity.HasOne(cs => cs.Course)
                    .WithMany(c => c.CourseSessions)
                    .HasForeignKey(cs => cs.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(cs => cs.Id)
                    .ValueGeneratedOnAdd();
            });

            builder.Entity<Enrollment>(entity =>
            {
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Employee)
                    .WithMany(emp => emp.Enrollments)
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EnrollmentRequest>(entity =>
            {
                entity.HasOne(er => er.Employee)
                    .WithMany(e => e.EnrollmentRequests)
                    .HasForeignKey(er => er.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(er => er.Course)
                    .WithMany(c => c.EnrollmentRequests)
                    .HasForeignKey(er => er.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(er => er.Reviewer)
                    .WithMany()
                    .HasForeignKey(er => er.ReviewerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(er => er.Id)
                    .ValueGeneratedOnAdd();
            });

            builder.Entity<Attendance>(entity =>
            {
                entity.HasOne(a => a.Session)
                    .WithMany(s => s.Attendances)
                    .HasForeignKey(a => a.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Employee)
                    .WithMany(e => e.Attendances)
                    .HasForeignKey(a => a.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Recorder)
                    .WithMany()
                    .HasForeignKey(a => a.RecordedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(a => a.Id)
                    .ValueGeneratedOnAdd();
            });

            builder.Entity<CourseFeedback>(entity =>
            {
                entity.HasOne(cf => cf.Course)
                    .WithMany(c => c.CourseFeedbacks)
                    .HasForeignKey(cf => cf.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cf => cf.Employee)
                    .WithMany(e => e.CourseFeedbacks)
                    .HasForeignKey(cf => cf.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(cf => cf.Id)
                    .ValueGeneratedOnAdd();
            });

            // =============================================
            // SYSTEM
            // =============================================
            builder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Report>(entity =>
            {
                entity.HasOne(r => r.Generator)
                    .WithMany(e => e.GeneratedReports)
                    .HasForeignKey(r => r.GeneratedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
