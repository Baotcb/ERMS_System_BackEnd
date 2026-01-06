using ERMS.Application.Features.JobPostings.DTOs;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.JobPostings.Queries.GetJobPostingById
{
    public class GetJobPostingByIdHandler : IRequestHandler<GetJobPostingByIdQuery, JobPostingDto>
    {
        private readonly IERMSDbContext _context;

        public GetJobPostingByIdHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<JobPostingDto> Handle(GetJobPostingByIdQuery request, CancellationToken cancellationToken)
        {
            var jobPosting = await _context.JobPostings
                .Include(j => j.Department)
                .Include(j => j.Creator)
                    .ThenInclude(c => c.User)
                .Include(j => j.JobSkills)
                    .ThenInclude(js => js.Skill)
                .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

            if (jobPosting == null)
            {
                throw new System.Exception("Không tìm thấy bài đăng tuyển dụng.");
            }

            return new JobPostingDto
            {
                Id = jobPosting.Id,
                Title = jobPosting.Title,
                Description = jobPosting.Description,
                Requirements = jobPosting.Requirements,
                MinSalary = jobPosting.MinSalary,
                MaxSalary = jobPosting.MaxSalary,
                Currency = jobPosting.Currency,
                Location = jobPosting.Location,
                DepartmentId = jobPosting.DepartmentId,
                DepartmentName = jobPosting.Department?.DepartmentName,
                CreatorId = jobPosting.CreatorId,
                CreatorName = jobPosting.Creator?.User?.FullName,
                PostingType = jobPosting.PostingType,
                Status = jobPosting.Status,
                PublishDate = jobPosting.PublishDate,
                ExpiresAt = jobPosting.ExpiresAt,
                ViewCount = jobPosting.ViewCount,
                Skills = jobPosting.JobSkills.Select(js => new SkillDto
                {
                    Id = js.SkillId,
                    Name = js.Skill.Name,
                    Weight = js.Weight,
                    MinProficiency = js.MinProficiency
                }).ToList()
            };
        }
    }
}