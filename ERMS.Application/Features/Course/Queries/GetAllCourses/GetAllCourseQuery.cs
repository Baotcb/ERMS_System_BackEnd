using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Courses.Queries.GetAllCourses
{
    public sealed class GetAllCourseQuery : IRequest<GetAllCoursesResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }
        public string? Status { get; set; }
        public bool? IsMandatory { get; set; }
    }

    public sealed class GetAllCoursesResult
    {
        public List<CourseDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class CourseDto
    {
        public Guid Id { get; set; }

        public string CourseName { get; set; } = null!;

        public string CourseCode { get; set; } = null!;

        public string? Description { get; set; }

        public string? ThumbnailUrl { get; set; }

        public string TrainerEmail { get; set; } 

        public int? DurationMinutes { get; set; }

        public string? Level { get; set; }

        public string Status { get; set; } = null!;

        public bool IsMandatory { get; set; }

        public int? MaxEnrollments { get; set; }

        public DateTime? EnrollmentDeadline { get; set; }

        public DateTime? PublishedAt { get; set; }

        public int LessonCount { get; set; }

        public int EnrollmentCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}