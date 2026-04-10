using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Lessons.Queries.GetLessonProgress
{
    public sealed class GetLessonProgressByCourseQuery : IRequest<List<LessonProgressDto>>
    {
        public Guid CourseId { get; set; }
    }
}
