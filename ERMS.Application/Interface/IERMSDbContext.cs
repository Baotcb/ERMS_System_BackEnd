using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ERMS.Domain.Entities.Application;

namespace ERMS.Application.Interface
{
    public interface IERMSDbContext
    {
            // Core Platform Entities
        DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        DbSet<Enterprise> Enterprises { get; set; }
        DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        
        // Organization Entities
        DbSet<Department> Departments { get; set; }
        DbSet<Employee> Employees { get; set; }
        DbSet<Candidate> Candidates { get; set; }
        
        // Recruitment Flow Entities
        DbSet<RecruitmentPlan> RecruitmentPlans { get; set; }
        DbSet<PlanDetail> PlanDetails { get; set; }
        DbSet<ApprovalHistory> ApprovalHistories { get; set; }
        DbSet<JobPosting> JobPostings { get; set; }
        DbSet<ApplicationEntity> Applications { get; set; }
        DbSet<CVScreeningResult> CVScreeningResults { get; set; }
        DbSet<Interview> Interviews { get; set; }
        DbSet<InterviewParticipant> InterviewParticipants { get; set; }
        DbSet<Offer> Offers { get; set; }
        
        // Candidate Profile Entities
        DbSet<Skill> Skills { get; set; }
        DbSet<JobSkill> JobSkills { get; set; }
        DbSet<CandidateSkill> CandidateSkills { get; set; }
        DbSet<Education> Educations { get; set; }
        DbSet<WorkExperience> WorkExperiences { get; set; }
        DbSet<Resume> Resumes { get; set; }
        
        // System Entities
        DbSet<Notification> Notifications { get; set; }
        DbSet<SavedJob> SavedJobs { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
