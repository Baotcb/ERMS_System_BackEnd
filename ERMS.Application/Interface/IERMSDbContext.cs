using ERMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ERMS.Domain.Entities.Application;

namespace ERMS.Application.Interface
{
    public interface IERMSDbContext
    {
        // Enterprise & Subscription
        DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        DbSet<Enterprise> Enterprises { get; set; }

        // Core
        DbSet<Department> Departments { get; set; }
        DbSet<Employee> Employees { get; set; }
        DbSet<Candidate> Candidates { get; set; }
        DbSet<Skill> Skills { get; set; }

        // Recruitment
        DbSet<JobPosting> JobPostings { get; set; }
        DbSet<JobSkill> JobSkills { get; set; }
        DbSet<CandidateSkill> CandidateSkills { get; set; }
        DbSet<Resume> Resumes { get; set; }
        DbSet<Education> Educations { get; set; }
        DbSet<WorkExperience> WorkExperiences { get; set; }
        DbSet<ApplicationEntity> Applications { get; set; }
        DbSet<Interview> Interviews { get; set; }
        DbSet<Offer> Offers { get; set; }

        // Training
        DbSet<Course> Courses { get; set; }
        DbSet<CourseSkill> CourseSkills { get; set; }
        DbSet<CourseSession> CourseSessions { get; set; }
        DbSet<Enrollment> Enrollments { get; set; }
        DbSet<EnrollmentRequest> EnrollmentRequests { get; set; }
        DbSet<Attendance> Attendances { get; set; }
        DbSet<CourseFeedback> CourseFeedbacks { get; set; }

        // System
        DbSet<Notification> Notifications { get; set; }
        DbSet<Report> Reports { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
