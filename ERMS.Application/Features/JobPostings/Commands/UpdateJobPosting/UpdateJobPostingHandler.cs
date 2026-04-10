using ERMS.Application.Interface;
using ERMS.Domain.Constants.Recruitment;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Recruitment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;

public sealed class UpdateJobPostingHandler : IRequestHandler<UpdateJobPostingCommand, Unit>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateJobPostingHandler> _logger;

    public UpdateJobPostingHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateJobPostingHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateJobPostingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var userRoles = _currentUserService.Roles;
        if (userRoles == null || !userRoles.Contains(AppRoles.HRManager))
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền cập nhật tin tuyển dụng.");

        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

        var jobPosting = await _context.JobPostings
            .FirstOrDefaultAsync(jp =>
                jp.Id == request.Id
                && jp.EnterpriseId == enterpriseId
                && !jp.IsDeleted,
                cancellationToken)
            ?? throw new Exception($"Không tìm thấy tin tuyển dụng với ID {request.Id}.");

        // Kiểm tra trạng thái cho phép chỉnh sửa
        if (jobPosting.Status == JobPostingStatus.Closed || jobPosting.Status == JobPostingStatus.Archived)
            throw new Exception($"Không thể chỉnh sửa tin tuyển dụng có trạng thái '{jobPosting.Status}'.");

        // Trạng thái Published: từ chối các trường bị hạn chế
        if (JobPostingStatus.IsPublished(jobPosting.Status))
        {
            var restrictedFields = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.JobTitle)) restrictedFields.Add("JobTitle");
            if (!string.IsNullOrWhiteSpace(request.Requirements)) restrictedFields.Add("Requirements");
            if (!string.IsNullOrWhiteSpace(request.EmploymentType)) restrictedFields.Add("EmploymentType");
            if (!string.IsNullOrWhiteSpace(request.ExperienceLevel)) restrictedFields.Add("ExperienceLevel");
            if (!string.IsNullOrWhiteSpace(request.EducationLevel)) restrictedFields.Add("EducationLevel");

            if (restrictedFields.Count > 0)
                throw new Exception($"Không thể chỉnh sửa các trường sau khi tin đã được đăng: {string.Join(", ", restrictedFields)}.");
        }

        // Áp dụng thay đổi — null/rỗng nghĩa là không thay đổi
        var updatedFields = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.JobTitle))
        {
            jobPosting.JobTitle = request.JobTitle.Trim();
            updatedFields.Add("JobTitle");
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            jobPosting.Description = request.Description.Trim();
            updatedFields.Add("Description");
        }

        if (!string.IsNullOrWhiteSpace(request.Requirements))
        {
            jobPosting.Requirements = request.Requirements.Trim();
            updatedFields.Add("Requirements");
        }

        if (!string.IsNullOrWhiteSpace(request.Benefits))
        {
            jobPosting.Benefits = request.Benefits.Trim();
            updatedFields.Add("Benefits");
        }

        if (!string.IsNullOrWhiteSpace(request.EmploymentType))
        {
            jobPosting.EmploymentType = request.EmploymentType.Trim();
            updatedFields.Add("EmploymentType");
        }

        if (!string.IsNullOrWhiteSpace(request.ExperienceLevel))
        {
            jobPosting.ExperienceLevel = request.ExperienceLevel.Trim();
            updatedFields.Add("ExperienceLevel");
        }

        if (!string.IsNullOrWhiteSpace(request.EducationLevel))
        {
            jobPosting.EducationLevel = request.EducationLevel.Trim();
            updatedFields.Add("EducationLevel");
        }

        if (request.SalaryRangeMin.HasValue)
        {
            jobPosting.SalaryRangeMin = request.SalaryRangeMin.Value;
            updatedFields.Add("SalaryRangeMin");
        }

        if (request.SalaryRangeMax.HasValue)
        {
            jobPosting.SalaryRangeMax = request.SalaryRangeMax.Value;
            updatedFields.Add("SalaryRangeMax");
        }

        if (request.ShowSalary.HasValue)
        {
            jobPosting.ShowSalary = request.ShowSalary.Value;
            updatedFields.Add("ShowSalary");
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            jobPosting.Location = request.Location.Trim();
            updatedFields.Add("Location");
        }

        if (!string.IsNullOrWhiteSpace(request.RemoteOption))
        {
            jobPosting.RemoteOption = request.RemoteOption.Trim();
            updatedFields.Add("RemoteOption");
        }

        if (request.Quantity.HasValue)
        {
            jobPosting.Quantity = request.Quantity.Value;
            updatedFields.Add("Quantity");
        }

        if (request.ApplicationDeadline.HasValue)
        {
            if (request.ApplicationDeadline.Value <= DateTime.UtcNow)
                throw new Exception("Hạn nộp hồ sơ phải trong tương lai.");
            jobPosting.ApplicationDeadline = request.ApplicationDeadline.Value;
            updatedFields.Add("ApplicationDeadline");
        }

        // Kiểm tra chéo mức lương sau khi đã áp dụng tất cả thay đổi
        if (jobPosting.SalaryRangeMin.HasValue && jobPosting.SalaryRangeMax.HasValue
            && jobPosting.SalaryRangeMax.Value < jobPosting.SalaryRangeMin.Value)
        {
            throw new Exception("Mức lương tối đa phải >= mức lương tối thiểu.");
        }

        if (updatedFields.Count > 0)
        {
            jobPosting.UpdatedAt = DateTime.UtcNow;
            _context.ApprovalHistories.Add(new ApprovalHistory
            {
                EntityType = "JobPosting",
                EntityId = jobPosting.Id,
                Action = "Updated",
                PreviousStatus = jobPosting.Status,
                NewStatus = jobPosting.Status,
                PerformedById = userId,
                Note = $"Đã cập nhật: {string.Join(", ", updatedFields)}"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã cập nhật bài tuyển dụng {JobPostingId}", request.Id);
        return Unit.Value;
    }
}
