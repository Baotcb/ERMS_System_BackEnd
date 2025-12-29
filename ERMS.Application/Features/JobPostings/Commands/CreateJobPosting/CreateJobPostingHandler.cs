using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.JobPostings.Commands.CreateJobPosting
{
    public class CreateJobPostingHandler : IRequestHandler<CreateJobPostingCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateJobPostingHandler(IERMSDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

           



            var jobPosting = new JobPosting
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Requirements = request.Requirements,
                MinSalary = request.MinSalary,
                MaxSalary = request.MaxSalary,
                Currency = request.Currency,
                Location = request.Location,
                DepartmentId = request.DepartmentId,
                CreatorId = userId.Value,
                PostingType = request.PostingType,
                Status = "Draft",
                PublishDate = request.PublishDate,
                ExpiresAt = request.ExpiresAt
            };

            _context.JobPostings.Add(jobPosting);

            
            if (request.SkillIds != null && request.SkillIds.Any())
            {
                foreach (var skillId in request.SkillIds)
                {
                    _context.JobSkills.Add(new JobSkill
                    {
                        JobId = jobPosting.Id,
                        SkillId = skillId,
                        Weight = 1,
                        MinProficiency = 1
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return jobPosting.Id;
        }
    }
}