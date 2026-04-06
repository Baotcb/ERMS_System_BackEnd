using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Enrollments.Queries.GetTrainingPerformanceChart
{
    public sealed class GetTrainingPerformanceChartHandler
        : IRequestHandler<GetTrainingPerformanceChartQuery, List<TrainingPerformanceDto>>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetTrainingPerformanceChartHandler> _logger;

        public GetTrainingPerformanceChartHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            ILogger<GetTrainingPerformanceChartHandler> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<List<TrainingPerformanceDto>> Handle(
            GetTrainingPerformanceChartQuery request,
            CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();

            if (enterpriseId == null)
                throw new Exception("Người dùng không thuộc doanh nghiệp nào.");

            // 1. Lấy dữ liệu thô từ Database (Lọc theo Enterprise và chưa xóa)
            var rawData = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Course.EnterpriseId == enterpriseId &&
                            !e.IsDeleted &&
                            !e.Course.IsDeleted)
                .Select(e => new
                {
                    GroupName = request.GroupByLevel ? e.Course.Level : e.Course.CourseName,
                    IsCompleted = e.Status == "Completed" || e.Progress == 100
                })
                .ToListAsync(cancellationToken);

            // 2. Xử lý GroupBy và tính toán tỉ lệ trên Memory
            var result = rawData
                .GroupBy(x => x.GroupName ?? "N/A")
                .Select(g => new TrainingPerformanceDto
                {
                    Label = g.Key,
                    // Công thức: Math.Round((Passed / Total) * 100)
                    Value = (int)Math.Round((double)g.Count(x => x.IsCompleted) / g.Count() * 100)
                })
                .OrderByDescending(x => x.Value) // Sắp xếp theo hiệu suất giảm dần
                .ToList();

            _logger.LogInformation(
                "Đã tính toán biểu đồ hiệu suất cho doanh nghiệp {EnterpriseId}. GroupByLevel: {GroupByLevel}",
                enterpriseId, request.GroupByLevel);

            return result;
        }
    }
}