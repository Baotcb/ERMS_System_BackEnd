using MediatR;
using System.Collections.Generic;

namespace ERMS.Application.Features.Enrollments.Queries.GetTrainingPerformanceChart
{
    public sealed class GetTrainingPerformanceChartQuery : IRequest<List<TrainingPerformanceDto>>
    {
        /// <summary>
        /// True: Nhóm theo Course.Level
        /// False: Nhóm theo Course.CourseName
        /// </summary>
        public bool GroupByLevel { get; set; } = false;
    }
}