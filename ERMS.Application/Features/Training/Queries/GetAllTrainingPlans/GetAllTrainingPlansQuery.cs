using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Training.Queries.GetAllTrainingPlans
{
    public sealed class GetAllTrainingPlansQuery
        : IRequest<GetAllTrainingPlansResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? Status { get; set; }
    }

    public sealed class GetAllTrainingPlansResult
    {
        public List<TrainingPlanDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages =>
        PageSize <= 0 ? 0 :
        (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class TrainingPlanDto
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = null!;
        public string PlanCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal? TotalBudget { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string CreatedBy { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public string? ReviewNote { get; set; }
        public string? Description { get; set; }
        public int Year { get; set; }
        public int TotalCourses { get; set; }
    }
}