using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using ERMS.Domain.Entities.Identity;
using ERMS.Domain.Entities.Enterprise;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Skill;
using ERMS.Domain.Entities.Recruitment;
using ERMS.Domain.Entities.Candidate;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Training;
using ERMS.Domain.Entities.System;
using ApplicationEntity = ERMS.Domain.Entities.Application;

namespace ERMS.Application.Interface
{
    public interface IERMSDbContext
    {
        DatabaseFacade Database { get; }

        DbSet<User> Users { get; set; }

       
        DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        DbSet<Enterprise> Enterprises { get; set; }
        DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        DbSet<OwnershipTransfer> OwnershipTransfers { get; set; }

      
        DbSet<Department> Departments { get; set; }
        DbSet<Employee> Employees { get; set; }
        DbSet<JobPosition> JobPositions { get; set; }

        // ================= Skill =================
        DbSet<Skill> Skills { get; set; }
        DbSet<JobCompetency> JobCompetencies { get; set; }

        // ================= Recruitment =================
        DbSet<RecruitmentCampaign> RecruitmentCampaigns { get; set; }
        DbSet<RecruitmentPlan> RecruitmentPlans { get; set; }
        DbSet<PlanDetail> PlanDetails { get; set; }
        DbSet<JobPosting> JobPostings { get; set; }
        DbSet<JobSkill> JobSkills { get; set; }
        DbSet<ApprovalHistory> ApprovalHistories { get; set; }

        // ================= Candidate =================
        DbSet<Candidate> Candidates { get; set; }
        DbSet<Education> Educations { get; set; }
        DbSet<WorkExperience> WorkExperiences { get; set; }
        DbSet<CandidateSkill> CandidateSkills { get; set; }
        DbSet<Resume> Resumes { get; set; }
        DbSet<SavedJob> SavedJobs { get; set; }

        // ================= Application =================
        DbSet<ApplicationEntity.Application> Applications { get; set; }
        DbSet<CVScreeningResult> CVScreeningResults { get; set; }
        DbSet<Interview> Interviews { get; set; }
        DbSet<InterviewParticipant> InterviewParticipants { get; set; }
        DbSet<Offer> Offers { get; set; }

        
        DbSet<TrainingPlan> TrainingPlans { get; set; }
        DbSet<TrainingRequest> TrainingRequests { get; set; }
        DbSet<Course> Courses { get; set; }
        DbSet<CourseSkill> CourseSkills { get; set; }
        DbSet<Lesson> Lessons { get; set; }
        DbSet<Enrollment> Enrollments { get; set; }
        DbSet<LessonProgress> LessonProgresses { get; set; }
        DbSet<Quiz> Quizzes { get; set; }
        DbSet<QuizQuestion> QuizQuestions { get; set; }
        DbSet<QuizAttempt> QuizAttempts { get; set; }
        DbSet<QuizAnswer> QuizAnswers { get; set; }
        DbSet<CourseFeedback> CourseFeedbacks { get; set; }
        DbSet<CourseFeedbackReply> CourseFeedbackReplies { get; set; }
        DbSet<WorkshopConfirmation> WorkshopConfirmations { get; set; }


      
        DbSet<Notification> Notifications { get; set; }

        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
