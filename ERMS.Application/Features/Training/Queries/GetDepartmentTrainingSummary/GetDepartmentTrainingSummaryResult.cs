using System;

namespace ERMS.Application.Features.Training.Queries.GetDepartmentTrainingSummary
{
    public sealed class GetDepartmentTrainingSummaryResult
    {
        public DepartmentTrainingSummaryDto Data { get; set; } = new();
    }

    public sealed class DepartmentTrainingSummaryDto
    {
        public int DepartmentId { get; set; }

        public int TotalEmployees { get; set; }

        public int TotalCourses { get; set; }

        public int StudiedEmployees { get; set; }

        public int PassedEmployees { get; set; }

        public int FailedEmployees { get; set; }
    }
}