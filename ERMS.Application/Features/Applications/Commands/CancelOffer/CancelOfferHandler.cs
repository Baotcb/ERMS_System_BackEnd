using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ERMS.Application.Features.Applications.Commands.CancelOffer;

/// <summary>
/// Handler for cancelling a job offer.
/// Only HR Managers belonging to the same enterprise can cancel an offer.
/// Cancellable statuses: Sent, Accepted.
/// </summary>
public sealed class CancelOfferHandler : IRequestHandler<CancelOfferCommand, CancelOfferResult>
{
    private readonly IERMSDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CancelOfferHandler> _logger;

    public CancelOfferHandler(
        IERMSDbContext context,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        ILogger<CancelOfferHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CancelOfferResult> Handle(CancelOfferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        // 2. Role check: HRManager ONLY
        if (!_currentUserService.Roles.Contains(AppRoles.HRManager))
        {
            throw new UnauthorizedAccessException("Chỉ HR Manager mới có quyền hủy offer.");
        }

        // 3. Resolve enterprise
        var enterpriseId = await _currentUserService.GetEnterpriseIdAsync()
            ?? throw new UnauthorizedAccessException("Không tìm thấy thông tin doanh nghiệp.");

        // 4. Load Offer with required navigation properties
        var offer = await _context.Offers
            .Include(o => o.Application)
                .ThenInclude(a => a.JobPosting)
            .Include(o => o.Application)
                .ThenInclude(a => a.Candidate)
                    .ThenInclude(c => c.User)
            .Include(o => o.Application)
                .ThenInclude(a => a.ExternalCandidate)
            .FirstOrDefaultAsync(o => o.Id == request.OfferId && !o.IsDeleted, cancellationToken)
            ?? throw new Exception($"Không tìm thấy offer với ID {request.OfferId}.");

        // 5. Enterprise ownership check
        if (offer.Application.JobPosting.EnterpriseId != enterpriseId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy offer này.");
        }

        // 6. Validate offer status — only Sent and Accepted can be cancelled
        if (!OfferStatus.CanCancel(offer.Status))
        {
            throw new Exception($"Offer này không thể bị hủy. Trạng thái hiện tại là '{offer.Status}'. Chỉ offer có trạng thái 'Sent' hoặc 'Accepted' mới có thể hủy.");
        }

        // 7. Cannot cancel if candidate is already Hired
        if (offer.Application.Stage == ApplicationStage.Hired)
        {
            throw new Exception("Không thể hủy offer vì ứng viên đã được tuyển dụng (Hired).");
        }

        var cancelledAt = DateTime.UtcNow;

        // 8. Update Offer status
        offer.Status = OfferStatus.Cancelled;
        offer.UpdatedAt = cancelledAt;

        // 9. Update Application stage to Rejected
        var application = offer.Application;
        application.Stage = ApplicationStage.Rejected;
        application.StageUpdatedAt = cancelledAt;
        application.UpdatedAt = cancelledAt;

        // 10. Persist changes — MUST complete before sending email
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Offer {OfferId} đã bị hủy bởi HR Manager {UserId}. Ứng tuyển {ApplicationId} chuyển sang trạng thái {Stage}.",
            offer.Id,
            userId,
            application.Id,
            application.Stage);

        // 11. Send cancellation email — failure is logged but does not roll back the cancellation
        await SendCancellationEmailAsync(offer, application, request.CancellationReason);

        return new CancelOfferResult
        {
            OfferId = offer.Id,
            NewOfferStatus = OfferStatus.Cancelled,
            NewApplicationStage = ApplicationStage.Rejected,
            CancelledAt = cancelledAt
        };
    }

    private async Task SendCancellationEmailAsync(
        Domain.Entities.Application.Offer offer,
        Domain.Entities.Application.Application application,
        string cancellationReason)
    {
        var candidateUser = application.Candidate.User;
        var recipientName = application.ExternalCandidate?.FullName ?? candidateUser.FullName;
        var recipientEmail = application.ExternalCandidate?.Email ?? candidateUser.Email;
        var emailSubject = $"Thông báo hủy offer - {offer.Position}";

        var encodedName = WebUtility.HtmlEncode(recipientName);
        var encodedPosition = WebUtility.HtmlEncode(offer.Position);
        var encodedReason = WebUtility.HtmlEncode(cancellationReason);

        var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .reason-box {{ background-color: white; padding: 20px; margin: 20px 0; border-left: 4px solid #f44336; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Thông báo hủy offer</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{encodedName}</strong>,</p>

            <p>Chúng tôi rất tiếc phải thông báo rằng offer cho vị trí <strong>{encodedPosition}</strong> đã bị hủy.</p>

            <div class='reason-box'>
                <h3>Lý do hủy offer:</h3>
                <p>{encodedReason}</p>
            </div>

            <p>Chúng tôi xin lỗi vì sự bất tiện này và mong bạn sẽ tìm được cơ hội phù hợp trong tương lai.</p>

            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với bộ phận HR của chúng tôi.</p>

            <p>Trân trọng,<br>
            <strong>Phòng Nhân sự</strong><br>
            <strong>ERMS System</strong></p>
        </div>
        <div class='footer'>
            <p>📧 Email này được gửi tự động từ hệ thống ERMS. Vui lòng không trả lời trực tiếp email này.</p>
            <p style='margin-top: 10px;'>© 2026 ERMS System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning("Bỏ qua việc gửi email hủy offer {OfferId} vì không có email ứng viên.", offer.Id);
            return;
        }

        try
        {
            await _emailService.SendEmailAsync(recipientEmail, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gửi email hủy offer {OfferId} đến {Email} thất bại.", offer.Id, recipientEmail);
        }
    }
}
