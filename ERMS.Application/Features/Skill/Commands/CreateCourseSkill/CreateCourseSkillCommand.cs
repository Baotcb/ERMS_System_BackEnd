using MediatR;
using System;

namespace ERMS.Application.Features.CourseSkills.Commands.CreateCourseSkill
{
    public sealed class CreateCourseSkillCommand : IRequest<Guid>
    {
        public Guid CourseId { get; set; }

        public Guid SkillId { get; set; }

        public int? SkillLevelGained { get; set; }
    }
}