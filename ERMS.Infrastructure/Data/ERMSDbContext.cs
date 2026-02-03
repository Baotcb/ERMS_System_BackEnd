using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Skill;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Candidate;
using ApplicationEntities = ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Training;
using ERMS.Domain.Entities.System;

namespace ERMS.Infrastructure.Data
{
    public class ERMSDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IERMSDbContext
    {
        public ERMSDbContext(DbContextOptions<ERMSDbContext> options)
            : base(options)
        {
        }

      
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Enterprise> Enterprises { get; set; }
        public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        public DbSet<OwnershipTransfer> OwnershipTransfers { get; set; }

       
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<JobPosition> JobPositions { get; set; }


        public DbSet<Skill> Skills { get; set; }
        public DbSet<JobCompetency> JobCompetencies { get; set; }


        public DbSet<RecruitmentCampaign> RecruitmentCampaigns { get; set; }
        public DbSet<RecruitmentPlan> RecruitmentPlans { get; set; }
        public DbSet<PlanDetail> PlanDetails { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobSkill> JobSkills { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }

       
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<CandidateSkill> CandidateSkills { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }


        public DbSet<ApplicationEntities.Application> Applications { get; set; }
        public DbSet<ApplicationEntities.CVScreeningResult> CVScreeningResults { get; set; }
        public DbSet<ApplicationEntities.Interview> Interviews { get; set; }
        public DbSet<ApplicationEntities.InterviewParticipant> InterviewParticipants { get; set; }
        public DbSet<ApplicationEntities.Offer> Offers { get; set; }

    
        public DbSet<TrainingPlan> TrainingPlans { get; set; }
        public DbSet<TrainingRequest> TrainingRequests { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSkill> CourseSkills { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }

    
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

  
            foreach (var property in builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

       
            builder.Entity<Enterprise>()
                .HasOne(e => e.SubscriptionPlan)
                .WithMany(s => s.Enterprises)
                .HasForeignKey(e => e.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enterprise>()
                .Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Inactive");


            builder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(m => m.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.UserId);

       
            builder.Entity<Candidate>()
                .HasOne(c => c.User)
                .WithOne(u => u.Candidate)
                .HasForeignKey<Candidate>(c => c.UserId);

        
            builder.Entity<JobPosting>()
                .HasOne(j => j.CreatedBy)
                .WithMany()
                .HasForeignKey(j => j.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<JobPosting>()
                .HasOne(j => j.PublishedBy)
                .WithMany()
                .HasForeignKey(j => j.PublishedById)
                .OnDelete(DeleteBehavior.Restrict);

         
            builder.Entity<ApplicationEntities.Application>()
                .HasOne(a => a.JobPosting)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobPostingId)
                .OnDelete(DeleteBehavior.Restrict);

           
            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Quiz)
                .WithMany()
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Restrict);

          
            builder.Entity<OwnershipTransfer>()
                .HasOne(o => o.FromUser)
                .WithMany()
                .HasForeignKey(o => o.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OwnershipTransfer>()
                .HasOne(o => o.ToUser)
                .WithMany()
                .HasForeignKey(o => o.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OwnershipTransfer>()
                .HasOne(o => o.ApprovedBy)
                .WithMany()
                .HasForeignKey(o => o.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentCampaign>()
                .HasOne(c => c.Enterprise)
                .WithMany()
                .HasForeignKey(c => c.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentCampaign>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentCampaign>()
                .HasIndex(c => new { c.EnterpriseId, c.CampaignCode })
                .IsUnique()
                .HasDatabaseName("UQ_RC_Enterprise_Code");

            builder.Entity<RecruitmentCampaign>()
                .Property(c => c.CampaignName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Entity<RecruitmentCampaign>()
                .Property(c => c.CampaignCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Entity<RecruitmentCampaign>()
                .Property(c => c.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Draft");

            builder.Entity<RecruitmentPlan>()
                .HasOne(r => r.Campaign)
                .WithMany(c => c.RecruitmentPlans)
                .HasForeignKey(r => r.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentPlan>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentPlan>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentPlan>()
                .Property(r => r.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Draft");

            builder.Entity<RecruitmentPlan>()
                .Property(r => r.RejectionReason)
                .HasMaxLength(1000);

            builder.Entity<RecruitmentPlan>()
                .HasOne(r => r.Enterprise)
                .WithMany()
                .HasForeignKey(r => r.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RecruitmentPlan>()
                .HasOne(r => r.Department)
                .WithMany()
                .HasForeignKey("DepartmentId") // Shadow property
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<PlanDetail>()
                .HasOne(p => p.RecruitmentPlan)
                .WithMany(r => r.PlanDetails)
                .HasForeignKey(p => p.RecruitmentPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlanDetail>()
                .HasOne(p => p.RequestedBy)
                .WithMany()
                .HasForeignKey(p => p.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PlanDetail>()
                .HasOne(p => p.Reviewer)
                .WithMany()
                .HasForeignKey(p => p.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<TrainingPlan>()
                .HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TrainingPlan>()
                .HasOne(t => t.ApprovedBy)
                .WithMany()
                .HasForeignKey(t => t.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TrainingRequest>()
                .HasOne(t => t.RequestedBy)
                .WithMany()
                .HasForeignKey(t => t.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationEntities.Offer>()
                .HasOne(o => o.CreatedBy)
                .WithMany()
                .HasForeignKey(o => o.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationEntities.Offer>()
                .HasOne(o => o.ApprovedBy)
                .WithMany()
                .HasForeignKey(o => o.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationEntities.Offer>()
                .HasOne(o => o.SentBy)
                .WithMany()
                .HasForeignKey(o => o.SentById)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<ApplicationEntities.Interview>()
                .HasOne(i => i.ScheduledBy)
                .WithMany()
                .HasForeignKey(i => i.ScheduledById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApprovalHistory>()
                .HasOne(a => a.PerformedBy)
                .WithMany()
                .HasForeignKey(a => a.PerformedById)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Additional FK Fixes for Multiple Cascade Paths ---
            
            // Employee: Enterprise -> Department -> Employee vs Enterprise -> Employee
            builder.Entity<Employee>()
                .HasOne(e => e.Enterprise)
                .WithMany()
                .HasForeignKey(e => e.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobPosting: Enterprise -> Department -> JobPosting vs Enterprise -> JobPosting
            builder.Entity<JobPosting>()
                .HasOne(j => j.Enterprise)
                .WithMany()
                .HasForeignKey(j => j.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // PlanDetail: Enterprise -> RecruitmentPlan -> PlanDetail vs Enterprise -> Department -> PlanDetail (PlanDetail -> Department relationship removed)


            // TrainingRequest: Enterprise -> Department -> TrainingRequest vs Enterprise -> TrainingRequest
            builder.Entity<TrainingRequest>()
                .HasOne(t => t.Enterprise)
                .WithMany()
                .HasForeignKey(t => t.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

        
            builder.Entity<Course>()
                .HasOne(c => c.Enterprise)
                .WithMany()
                .HasForeignKey(c => c.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Course>()
                .HasOne(c => c.Trainer)
                .WithMany()
                .HasForeignKey(c => c.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

           
            builder.Entity<Enrollment>()
                .HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

     
            builder.Entity<ApplicationEntities.Application>()
                .HasOne(a => a.Candidate)
                .WithMany(c => c.Applications)
                .HasForeignKey(a => a.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
