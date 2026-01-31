using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    /// <summary>
    /// Command để cập nhật thông tin doanh nghiệp
    /// Chỉ cho phép HR cập nhật: EnterpriseName, Address, Phone, Website
    /// Mỗi lần cập nhật phải cách nhau ít nhất 6 tháng
    /// </summary>
    public class UpdateEnterpriseCommand : IRequest<bool>
    {
        [Required(ErrorMessage = "Id doanh nghiệp là bắt buộc")]
        public Guid Id { get; set; }

        /// <summary>
        /// Tên doanh nghiệp mới (phải unique và không trùng với doanh nghiệp lớn)
        /// </summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên doanh nghiệp phải từ 2-200 ký tự")]
        public string? EnterpriseName { get; set; }

        /// <summary>
        /// Địa chỉ doanh nghiệp
        /// </summary>
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        /// <summary>
        /// Số điện thoại doanh nghiệp
        /// </summary>
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        /// <summary>
        /// Website doanh nghiệp
        /// </summary>
        [Url(ErrorMessage = "Website không hợp lệ")]
        [StringLength(200, ErrorMessage = "Website không được vượt quá 200 ký tự")]
        public string? Website { get; set; }
    }
}
