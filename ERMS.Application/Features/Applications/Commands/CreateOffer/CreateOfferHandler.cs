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
           if(!_currentUserService.Roles.Contains(AppRoles.HRManager))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tạo offer.");
            }
           
            var application = await _context.Applications
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User)
                .Include(a => a.JobPosting)
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && !a.IsDeleted, cancellationToken);

            if (application == null)
            {
                throw new Exception("Không tìm thấy đơn ứng tuyển.");
            }

            if (application.Offer != null && !application.Offer.IsDeleted)
            {
                throw new Exception("Đơn ứng tuyển này đã có offer.");
            }

            if (application.Stage != ApplicationStage.OfferProcessing)
            {
                throw new Exception("Đơn ứng tuyển phải ở trạng thái 'OfferProcessing' để tạo offer.");
            }

            if(_currentUserService.GetEnterpriseIdAsync== null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin doanh nghiệp.");
            }
            var enterpriseId = await _currentUserService.GetEnterpriseIdAsync();
            if (application.JobPosting.EnterpriseId != enterpriseId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tạo offer cho đơn ứng tuyển này.");
            }
            var department = await _currentUserService.GetDepartmentIdAsync();
            if(department == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin phòng ban.");
            }
            if (application.JobPosting.DepartmentId != department.Value)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tạo offer cho đơn ứng tuyển này.");
            }

   
            if (request.StartDate < DateTime.UtcNow.Date)
            {
                throw new Exception("Ngày bắt đầu phải từ hôm nay trở đi.");
            }

            if (request.ExpirationDate <= DateTime.UtcNow)
            {
                throw new Exception("Ngày hết hạn phải sau thời điểm hiện tại.");
            }


            var offerCode = await GenerateOfferCodeAsync(cancellationToken);

    
            var offer = new Offer
            {
                Id = Guid.NewGuid(),
                ApplicationId = request.ApplicationId,
                OfferCode = offerCode,
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
                CreatedById = currentUserId.Value,
                SentById = currentUserId.Value,
                SentAt = DateTime.UtcNow,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            application.Stage = ApplicationStage.Offered;
            application.StageUpdatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

  
            await _context.Offers.AddAsync(offer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await SendOfferEmailAsync(offer, application, cancellationToken);

            return offer.Id;
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

            int nextNumber = 1;
            if (lastOffer != null && !string.IsNullOrEmpty(lastOffer.OfferCode))
            {
                var lastNumberStr = lastOffer.OfferCode.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        private async Task SendOfferEmailAsync(Offer offer, Domain.Entities.Application.Application application, CancellationToken cancellationToken)
        {
            var candidate = application.Candidate;
            var candidateUser = candidate.User;
            var jobTitle = application.JobPosting.Description;

            var token = await _userManager.GenerateUserTokenAsync(
                candidateUser,
                TokenOptions.DefaultProvider,
                $"OfferAccess-{offer.Id}");

            var encodedToken = Uri.EscapeDataString(token);
            var encodedOfferId = Uri.EscapeDataString(offer.Id.ToString());

       
            var clientUrl = _configuration["ClientSettings:Url"] ?? "http://localhost:3000";
            var offerDetailUrl = $"{clientUrl}/my-offers?offerId={encodedOfferId}&token={encodedToken}";

            var emailSubject = $"🎉 Thư mời nhận việc - {offer.Position}";

            var bonusText = !string.IsNullOrWhiteSpace(offer.Bonus)
                ? $"<p><strong>Thưởng:</strong> {offer.Bonus}</p>"
                : "";

            var benefitsText = !string.IsNullOrWhiteSpace(offer.Benefits)
                ? $"<p><strong>Phúc lợi:</strong> {offer.Benefits}</p>"
                : "";

            var offerLetterLink = !string.IsNullOrWhiteSpace(offer.OfferLetterUrl)
                ? $@"<p><strong>Thư mời chính thức:</strong> <a href='{offer.OfferLetterUrl}' style='color: #4CAF50;'>Tải xuống PDF</a></p>"
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
        .button {{ background-color: #4CAF50; color: white; padding: 12px 30px; text-decoration: none; 
                   border-radius: 5px; display: inline-block; margin: 10px 5px; font-weight: bold; }}
        .button:hover {{ background-color: #45a049; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        .urgent {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .security-note {{ background-color: #e3f2fd; border-left: 4px solid #2196F3; padding: 15px; margin: 20px 0; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chúc mừng!</h1>
            <p>Bạn đã nhận được thư mời nhận việc</p>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{candidateUser.FullName}</strong>,</p>
            
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
                {offerLetterLink}
            </div>

            <div class='urgent'>
                ⚠️ <strong>Quan trọng:</strong> Vui lòng phản hồi đề nghị này trước ngày <strong>{offer.ExpirationDate:dd/MM/yyyy HH:mm}</strong>.
            </div>
            
            <div class='security-note'>
                🔒 <strong>Bảo mật:</strong> Link dưới đây được tạo riêng cho bạn và có thời hạn sử dụng. 
                Vui lòng không chia sẻ link này với người khác.
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <p style='font-size: 16px; margin-bottom: 20px;'><strong>Bạn có thể xem chi tiết và phản hồi offer ngay tại đây:</strong></p>
                <a href='{offerDetailUrl}' class='button'>📄 Xem chi tiết & Phản hồi Offer</a>
                <p style='font-size: 12px; color: #666; margin-top: 15px;'>Hoặc copy link sau vào trình duyệt:</p>
                <p style='font-size: 12px; color: #4CAF50; word-break: break-all;'>{offerDetailUrl}</p>
            </div>

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

            try
            {
                await _emailService.SendEmailAsync(candidateUser.Email, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
    }
}