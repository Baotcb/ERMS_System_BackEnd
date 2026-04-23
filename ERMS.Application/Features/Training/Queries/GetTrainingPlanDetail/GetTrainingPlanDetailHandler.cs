using ERMS.Application.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Training.Queries.GetTrainingPlanDetail
{
    public sealed class GetTrainingPlanDetailHandler
        : IRequestHandler<GetTrainingPlanDetailQuery, TrainingPlanDetailDto>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetTrainingPlanDetailHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<TrainingPlanDetailDto> Handle(
            GetTrainingPlanDetailQuery request,
            CancellationToken cancellationToken)
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
                ?? throw new Exception("Người dùng không thuộc doanh nghiệp nào");

            var plan = await _context.TrainingPlans
                .Where(p =>
                    p.Id == request.Id &&
                    p.EnterpriseId == enterpriseId &&
                    !p.IsDeleted)
                .Select(p => new TrainingPlanDetailDto
                {
                    Id = p.Id,
                    PlanName = p.PlanName,
                    PlanCode = p.PlanCode,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    TotalBudget = p.TotalBudget,
                    Status = p.Status,
                    ReviewNote = p.ReviewNote,

                    TrainingRequests = p.TrainingRequests
                        .Select(r => new TrainingRequestDto
                        {
                            Id = r.Id,
                            Subject = r.Subject,
                            RequestedByName = r.RequestedBy.FullName,
                            DepartmentName = r.Department.DepartmentName,
                            TargetAudience = r.TargetAudience,
                            EstimatedParticipants = r.EstimatedParticipants,
                            Status = r.Status
                        }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (plan == null)
                throw new Exception("Không tìm thấy kế hoạch đào tạo");

            return plan;
        }
    }
}