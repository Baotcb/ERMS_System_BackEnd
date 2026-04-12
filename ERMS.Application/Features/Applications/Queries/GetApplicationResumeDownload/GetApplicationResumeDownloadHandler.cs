using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Application.Features.Applications.Queries.GetApplicationResumeDownload;

public sealed class GetApplicationResumeDownloadHandler
    : IRequestHandler<GetApplicationResumeDownloadQuery, GetApplicationResumeDownloadResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICloudinaryService _cloudinaryService;

    public GetApplicationResumeDownloadHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        ICloudinaryService cloudinaryService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<GetApplicationResumeDownloadResult> Handle(
        GetApplicationResumeDownloadQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var roles = (_currentUserService.Roles ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roles.Count == 0)
        {
            throw new UnauthorizedAccessException("Người dùng không có quyền tải CV.");
        }

        var application = await _context.Applications
            .Where(a => a.Id == request.ApplicationId && !a.IsDeleted)
            .Select(a => new
            {
                CandidateUserId = a.Candidate.UserId,
                EnterpriseId = a.JobPosting.EnterpriseId,
                DepartmentId = a.JobPosting.DepartmentId,
                ResumeUrl = a.Resume != null ? a.Resume.FileUrl : null,
                ResumeFileName = a.Resume != null ? a.Resume.FileName : null
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Exception($"Không tìm thấy hồ sơ ứng tuyển với ID {request.ApplicationId}.");

        if (string.IsNullOrWhiteSpace(application.ResumeUrl) || string.IsNullOrWhiteSpace(application.ResumeFileName))
        {
            throw new Exception("Ứng viên chưa có CV để tải.");
        }

        if (roles.Contains(AppRoles.HRManager) || roles.Contains(AppRoles.Director))
        {
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
                ?? throw new UnauthorizedAccessException("Người dùng không thuộc doanh nghiệp nào.");

            if (enterpriseId != application.EnterpriseId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có quyền tải CV của ứng viên thuộc doanh nghiệp mình.");
            }
        }
        else if (roles.Contains(AppRoles.DepartmentHead))
        {
            var departmentId = await _currentUserService.GetDepartmentIdAsync()
                ?? throw new UnauthorizedAccessException("Người dùng không thuộc phòng ban nào.");

            if (departmentId != application.DepartmentId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có quyền tải CV của ứng viên thuộc phòng ban mình.");
            }
        }
        else if (roles.Contains(AppRoles.Candidate))
        {
            if (application.CandidateUserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ có quyền tải CV của chính mình.");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Người dùng không có quyền tải CV.");
        }

        return new GetApplicationResumeDownloadResult
        {
            DownloadUrl = _cloudinaryService.GetAuthenticatedDownloadUrl(
                application.ResumeUrl,
                application.ResumeFileName,
                TimeSpan.FromMinutes(5))
        };
    }
}
