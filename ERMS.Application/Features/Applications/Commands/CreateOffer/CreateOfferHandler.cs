using ERMS.Application.Exceptions;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Application;
using ERMS.Domain.Constants.Roles;
using ERMS.Domain.Entities.Application;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ERMS.Application.Features.Applications.Commands.CreateOffer
{
    public class CreateOfferHandler : IRequestHandler<CreateOfferCommand, Guid>
    {
        private readonly IERMSDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;

        public CreateOfferHandler(
            IERMSDbContext context,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IConfiguration configuration,
            UserManager<User> userManager)
        {
            _context = context;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _configuration = configuration;
            _userManager = userManager;
        }

                public async Task<Guid> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng.");
            }

            if (!_currentUserService.Roles.Contains(AppRoles.HRManager))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tạo offer.");
            }

            var application = await _context.Applications
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User)
                .Include(a => a.ExternalCandidate)
                .Include(a => a.JobPosting)
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken);

            if (application == null)
            {
                throw new BusinessException("Không tìm thấy đơn ứng tuyển.");
            }

            if (application.Offer != null && !application.Offer.IsDeleted)
            {
                throw new BusinessException("Đơn ứng tuyển này đã có offer.");
            }

            if (application.Stage != ApplicationStage.OfferProcessing)
            {
                throw new BusinessException("Đơn ứng tuyển phải ở trạng thái 'OfferProcessing' để tạo offer.");
            }

            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (enterpriseId == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin doanh nghiệp.");
            }

            if (application.JobPosting.EnterpriseId != enterpriseId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tạo offer cho đơn ứng tuyển này.");
            }

            var department = await _currentUserService.GetDepartmentIdAsync();
            if (department == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin phòng ban.");
            }

            if (request.StartDate < DateTime.UtcNow.Date)
            {
                throw new BusinessException("Ngày bắt đầu phải từ hôm nay trở đi.");
            }

            if (request.ExpirationDate <= DateTime.UtcNow)
            {
                throw new BusinessException("Ngày hết hạn phải sau thời điểm hiện tại.");
            }

            if (request.ExpirationDate >= request.StartDate)
            {
                throw new BusinessException("Hạn phản hồi offer phải trước ngày bắt đầu làm việc.");
            }

            var offerCode = await GenerateOfferCodeAsync(cancellationToken);
            var isExternalApplication = application.ExternalCandidateId != null && application.ExternalCandidate != null;
            var isNewOffer = application.Offer == null;
            var isReusingSoftDeletedOffer = !isNewOffer && application.Offer!.IsDeleted;
            var now = DateTime.UtcNow;
            var responseToken = isExternalApplication ? Guid.NewGuid().ToString("N") : null;
            var resolvedOfferCode = isReusingSoftDeletedOffer && !string.IsNullOrWhiteSpace(application!.Offer!.OfferCode)
                ? application.Offer!.OfferCode!
                : offerCode;

            var offerToSend = new Offer
            {
                Id = isReusingSoftDeletedOffer ? application!.Offer!.Id : Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                OfferCode = resolvedOfferCode,
                Position = request.Position,
                DepartmentId = application.JobPosting.DepartmentId,
                Salary = request.Salary,
                SalaryFrequency = request.SalaryFrequency,
                Bonus = request.Bonus,
                Benefits = request.Benefits,
                StartDate = request.StartDate,
                ExpirationDate = request.ExpirationDate,
                OfferLetterUrl = request.OfferLetterUrl,
                Status = OfferStatus.Sent,
                CreatedById = isReusingSoftDeletedOffer && application!.Offer!.CreatedById != Guid.Empty
                    ? application.Offer!.CreatedById
                    : currentUserId.Value,
                CreatedAt = isReusingSoftDeletedOffer && application!.Offer!.CreatedAt != default
                    ? application.Offer!.CreatedAt
                    : now,
                SentById = currentUserId.Value,
                SentAt = now,
                IsDeleted = false,
                DeletedAt = null,
                UpdatedAt = now,
                ResponseToken = responseToken,
                TokenExpiresAt = isExternalApplication ? request.ExpirationDate : null
            };

            try
            {
                await SendOfferEmailAsync(offerToSend, application, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is BusinessException)
                {
                    throw;
                }

                throw new BusinessException($"Gửi email offer thất bại: {ex.Message}", ex);
            }

            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                Offer offerToPersist;

                if (isReusingSoftDeletedOffer)
                {
                    offerToPersist = application.Offer!;
                    offerToPersist.ApplicationId = offerToSend.ApplicationId;
                    offerToPersist.OfferCode = offerToSend.OfferCode;
                    offerToPersist.Position = offerToSend.Position;
                    offerToPersist.DepartmentId = offerToSend.DepartmentId;
                    offerToPersist.Salary = offerToSend.Salary;
                    offerToPersist.SalaryFrequency = offerToSend.SalaryFrequency;
                    offerToPersist.Bonus = offerToSend.Bonus;
                    offerToPersist.Benefits = offerToSend.Benefits;
                    offerToPersist.StartDate = offerToSend.StartDate;
                    offerToPersist.ExpirationDate = offerToSend.ExpirationDate;
                    offerToPersist.OfferLetterUrl = offerToSend.OfferLetterUrl;
                    offerToPersist.Status = offerToSend.Status;
                    offerToPersist.CreatedById = offerToPersist.CreatedById == Guid.Empty
                        ? offerToSend.CreatedById
                        : offerToPersist.CreatedById;
                    if (offerToPersist.CreatedAt == default)
                    {
                        offerToPersist.CreatedAt = offerToSend.CreatedAt;
                    }

                    offerToPersist.SentById = offerToSend.SentById;
                    offerToPersist.SentAt = offerToSend.SentAt;
                    offerToPersist.IsDeleted = false;
                    offerToPersist.DeletedAt = null;
                    offerToPersist.UpdatedAt = offerToSend.UpdatedAt;
                    offerToPersist.ResponseToken = offerToSend.ResponseToken;
                    offerToPersist.TokenExpiresAt = offerToSend.TokenExpiresAt;
                }
                else
                {
                    offerToPersist = offerToSend;
                    application.Offer = offerToPersist;
                    await _context.Offers.AddAsync(offerToPersist, cancellationToken);
                }

                var stageUpdatedAt = DateTime.UtcNow;
                application.Stage = ApplicationStage.Offered;
                application.StageUpdatedAt = stageUpdatedAt;
                application.UpdatedAt = stageUpdatedAt;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return offerToPersist.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                if (ex is BusinessException)
                {
                    throw;
                }

                throw new BusinessException($"Tạo offer thất bại sau khi gửi email: {ex.Message}", ex);
            }
        }

        private async Task<string> GenerateOfferCodeAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var prefix = $"OFF{year:D4}{month:D2}";

            var lastOffer = await _context.Offers
                .Where(o => o.OfferCode!.StartsWith(prefix))
                .OrderByDescending(o => o.OfferCode)
                .FirstOrDefaultAsync(cancellationToken);

            var nextNumber = 1;
            if (lastOffer != null && !string.IsNullOrEmpty(lastOffer.OfferCode))
            {
                var lastNumberStr = lastOffer.OfferCode.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out var lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        private async Task SendOfferEmailAsync(Offer offer, Domain.Entities.Application.Application application, CancellationToken cancellationToken)
        {
            var candidateUser = application.Candidate?.User;
            var externalCandidate = application.ExternalCandidate;
            var isExternalApplication = application.ExternalCandidateId != null && externalCandidate != null;
            var recipientName = isExternalApplication
                ? externalCandidate!.FullName
                : candidateUser?.FullName;
            var recipientEmail = isExternalApplication
                ? externalCandidate!.Email
                : candidateUser?.Email;
            var jobTitle = application.JobPosting.Description;

            var emailSubject = $"🎉 Thư mời nhận việc - {offer.Position}";

            var bonusText = !string.IsNullOrWhiteSpace(offer.Bonus)
                ? $"<p><strong>Thưởng:</strong> {offer.Bonus}</p>"
                : "";

            var benefitsText = !string.IsNullOrWhiteSpace(offer.Benefits)
                ? $"<p><strong>Phúc lợi:</strong> {offer.Benefits}</p>"
                : "";

            var clientUrl = (_configuration["ClientSettings:Url"] ?? string.Empty).TrimEnd('/');
            var responseActionsHtml = isExternalApplication &&
                                      !string.IsNullOrWhiteSpace(offer.ResponseToken) &&
                                      !string.IsNullOrWhiteSpace(clientUrl)
                ? $@"
            <div style='margin: 24px 0; text-align: center;'>
                <a href='{clientUrl}/offer-response/{offer.ResponseToken}?action=accept' style='display: inline-block; margin: 0 8px; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px;'>Chấp nhận offer</a>
                <a href='{clientUrl}/offer-response/{offer.ResponseToken}?action=reject' style='display: inline-block; margin: 0 8px; padding: 12px 24px; background-color: #f44336; color: white; text-decoration: none; border-radius: 4px;'>Từ chối offer</a>
            </div>"
                : "";

            var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .offer-details {{ background-color: white; padding: 20px; margin: 20px 0; border-left: 4px solid #4CAF50; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chúc mừng!</h1>
            <p>Bạn đã nhận được thư mời nhận việc</p>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{recipientName}</strong>,</p>
            
            <p>Chúng tôi rất vui mừng thông báo rằng bạn đã được chọn cho vị trí <strong>{offer.Position}</strong> 
            tại công ty chúng tôi sau quá trình phỏng vấn cho công việc <strong>{jobTitle}</strong>.</p>
            
            <div class='offer-details'>
                <h3>📋 Chi tiết đề nghị:</h3>
                <p><strong>Mã offer:</strong> {offer.OfferCode}</p>
                <p><strong>Vị trí:</strong> {offer.Position}</p>
                <p><strong>Mức lương:</strong> {offer.Salary:N0} VNĐ / {offer.SalaryFrequency}</p>
                {bonusText}
                {benefitsText}
                <p><strong>Ngày bắt đầu:</strong> {offer.StartDate:dd/MM/yyyy}</p>
                <p><strong>Hạn phản hồi:</strong> <span style='color: #f44336; font-weight: bold;'>{offer.ExpirationDate:dd/MM/yyyy HH:mm}</span></p>
            </div>

            <div class='urgent'>
                ⚠️ <strong>Quan trọng:</strong> Vui lòng phản hồi đề nghị này trước ngày <strong>{offer.ExpirationDate:dd/MM/yyyy HH:mm}</strong>.
            </div>
            {responseActionsHtml}

            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với bộ phận HR của chúng tôi qua email này hoặc số điện thoại: <strong>1900-xxxx</strong>.</p>
            
            <p>Chúng tôi rất mong được làm việc cùng bạn!</p>
            
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
                throw new BusinessException("Không tìm thấy email ứng viên để gửi offer.");
            }

            await _emailService.SendEmailAsync(recipientEmail, emailSubject, emailBody);
        }
    }
}


