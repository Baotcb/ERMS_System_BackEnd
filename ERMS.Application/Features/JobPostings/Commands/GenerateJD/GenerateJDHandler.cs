using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.GenerateJD;

public sealed class GenerateJDHandler : IRequestHandler<GenerateJDCommand, GenerateJDResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAIService _aiService;
    private readonly ILogger<GenerateJDHandler> _logger;

    public GenerateJDHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IAIService aiService,
        ILogger<GenerateJDHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<GenerateJDResult> Handle(GenerateJDCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate user role
        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền tạo JD.");

        // 2. Resolve enterprise
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        // 3. Load PlanDetail with RecruitmentPlan, validate enterprise ownership
        var planDetail = await _context.PlanDetails
            .Include(pd => pd.RecruitmentPlan)
            .FirstOrDefaultAsync(pd =>
                pd.Id == request.PlanDetailId
                && pd.RecruitmentPlan.EnterpriseId == enterpriseId
                && !pd.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"Không tìm thấy chi tiết kế hoạch với ID {request.PlanDetailId}.");

        // 4. Validate parent RecruitmentPlan is Approved
        if (!PlanStatus.IsApproved(planDetail.RecruitmentPlan.Status))
            throw new Exception($"Kế hoạch tuyển dụng phải được phê duyệt trước khi tạo JD. Hiện tại: {planDetail.RecruitmentPlan.Status}");

        // 5. Call Gemini AI to generate JD
        try
        {
            var skillsArg = !string.IsNullOrWhiteSpace(request.UserPrompt) 
                ? request.UserPrompt 
                : planDetail.RequiredSkills;

            var jdResult = await _aiService.GenerateJobDescriptionAsync(
                planDetail.PositionTitle,
                planDetail.Justification,
                skillsArg,
                planDetail.MinExperience,
                planDetail.MaxExperience,
                planDetail.EducationLevel,
                planDetail.SalaryRangeMin,
                planDetail.SalaryRangeMax);

            _logger.LogInformation("Tạo JD thành công cho PlanDetail {PlanDetailId}", request.PlanDetailId);

            return new GenerateJDResult
            {
                Description = jdResult.Description,
                Requirements = jdResult.Requirements,
                Benefits = jdResult.Benefits,
                PlanDetailId = request.PlanDetailId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo JD cho PlanDetail {PlanDetailId}", request.PlanDetailId);
            throw new Exception("Không thể tạo JD. Vui lòng thử lại sau.");
        }
    }
}
