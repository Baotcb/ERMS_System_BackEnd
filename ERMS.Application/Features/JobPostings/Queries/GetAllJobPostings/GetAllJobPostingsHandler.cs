using ERMS.Application.Features.JobPostings.DTOs;
using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.JobPostings.Queries.GetAllJobPostings
{
    public class GetAllJobPostingsHandler : IRequestHandler<GetAllJobPostingsQuery, List<JobPostingDto>>
    {
        private readonly IERMSDbContext _context;

        public GetAllJobPostingsHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<List<JobPostingDto>> Handle(GetAllJobPostingsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.JobPostings
                .Include(j => j.Department)
                .Include(j => j.Creator)
                    .ThenInclude(c => c.User)
                .Include(j => j.JobSkills)
                    .ThenInclude(js => js.Skill)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(j => j.Status == request.Status);
            }

            // Filter by posting type
            if (!string.IsNullOrEmpty(request.PostingType))
            {
                query = query.Where(j => j.PostingType == request.PostingType);
            }

            // Pagination
            var jobPostings = await query
                .OrderByDescending(j => j.PublishDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return jobPostings.Select(j => new JobPostingDto
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Requirements = j.Requirements,
                MinSalary = j.MinSalary,
                MaxSalary = j.MaxSalary,
                Currency = j.Currency,
                Location = j.Location,
                DepartmentId = j.DepartmentId,
                DepartmentName = j.Department?.DepartmentName,
                CreatorId = j.CreatorId,
                CreatorName = j.Creator?.User?.FullName,
                PostingType = j.PostingType,
                Status = j.Status,
                PublishDate = j.PublishDate,
                ExpiresAt = j.ExpiresAt,
                ViewCount = j.ViewCount,
                Skills = j.JobSkills.Select(js => new SkillDto
                {
                    Id = js.SkillId,
                    Name = js.Skill.Name,
                    Weight = js.Weight,
                    MinProficiency = js.MinProficiency
                }).ToList()
            }).ToList();
        }
    }
}