using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.CourseSkills.Commands.CreateCourseSkill
{
    public sealed class CreateCourseSkillHandler : IRequestHandler<CreateCourseSkillCommand, Guid>
    {
        private readonly IERMSDbContext _context;

        public CreateCourseSkillHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateCourseSkillCommand request, CancellationToken cancellationToken)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == request.CourseId && !c.IsDeleted, cancellationToken);

            if (course == null)
                throw new KeyNotFoundException("Course not found");

            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Id == request.SkillId && !s.IsDeleted, cancellationToken);

            if (skill == null)
                throw new KeyNotFoundException("Skill not found");

            var exists = await _context.CourseSkills
                .AnyAsync(cs => cs.CourseId == request.CourseId && cs.SkillId == request.SkillId, cancellationToken);

            if (exists)
                throw new Exception("Skill already assigned to this course");

            var courseSkill = new CourseSkill
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                SkillId = request.SkillId,
                SkillLevelGained = request.SkillLevelGained
            };

            _context.CourseSkills.Add(courseSkill);

            await _context.SaveChangesAsync(cancellationToken);

            return courseSkill.Id;
        }
    }
}