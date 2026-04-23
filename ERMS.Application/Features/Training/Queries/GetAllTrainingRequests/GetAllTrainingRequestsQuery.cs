using MediatR;
using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Training.Queries.GetAllTrainingRequests
{
    public sealed class GetAllTrainingRequestsQuery
        : IRequest<GetAllTrainingRequestsResult>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? DepartmentId { get; set; }
        public string? Status { get; set; }
        public string? Urgency { get; set; }
    }

    public sealed class GetAllTrainingRequestsResult
    {
        public List<TrainingRequestDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class TrainingRequestDto
    {
        public Guid Id { get; set; }
        public string Subject { get; set; } = null!;
        public string Urgency { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string DepartmentName { get; set; } = null!;
        public string RequestedByName { get; set; } = null!;
        public string? TargetAudience { get; set; }
        public int? EstimatedParticipants { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}