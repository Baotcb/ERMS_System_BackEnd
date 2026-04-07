using ERMS.Domain.Entities;
using ERMS.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ERMS.Application.Features.Auth.Commands.ConfirmEmail
{
    public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, string>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ConfirmEmailHandler> _logger;

        public ConfirmEmailHandler(
            UserManager<User> userManager,
            ILogger<ConfirmEmailHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            try
            {
               
                if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Token))
                {
                    throw new ArgumentException("UserId và Token là bắt buộc.");
                }

                
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new ArgumentException("Tài khoản không tồn tại.");
                }

           
                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Người dùng {UserId} cố gắng xác nhận email đã được xác nhận trước đó", user.Id);
                    return "Email đã được xác thực trước đó. Bạn có thể đăng nhập bình thường.";
                }

             
                var result = await _userManager.ConfirmEmailAsync(user, request.Token);
                
                if (result.Succeeded)
                {
                   
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("Email được xác nhận thành công cho người dùng: {UserId} - {Email}", user.Id, user.Email);
                    
                    return "🎉 Email đã được xác thực thành công! Bây giờ bạn có thể đăng nhập vào hệ thống.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Xác nhận email thất bại cho người dùng {UserId}: {Errors}", user.Id, errors);
                    
                    throw new ArgumentException($"Xác thực email thất bại: {errors}. Có thể token đã hết hạn hoặc không hợp lệ.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác nhận email cho người dùng: {UserId}", request.UserId);
                throw;
            }
        }
    }
}