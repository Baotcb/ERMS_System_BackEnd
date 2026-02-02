using System;

namespace ERMS.Domain.Constants.Recruitment
{
    /// <summary>
    /// Trạng thái của Kế hoạch Tuyển dụng (RecruitmentPlan)
    /// </summary>
    public static class PlanStatus
    {
        /// <summary>
        /// ⏳ Pending: Dept Head đã submit, đang chờ HR phê duyệt
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// ✅ Approved: HR đã phê duyệt, có thể tạo JobPosting
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// ❌ Rejected: HR từ chối, cần chỉnh sửa hoặc hủy
        /// </summary>
        public const string Rejected = "Rejected";

        public static readonly string[] ValidStatuses = { Pending, Approved, Rejected };

        /// <summary>
        /// Kiểm tra trạng thái có hợp lệ không
        /// </summary>
        public static bool IsValid(string status)
        {
            return Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kiểm tra plan đã được phê duyệt chưa
        /// </summary>
        public static bool IsApproved(string status)
        {
            return status.Equals(Approved, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra plan đang chờ phê duyệt không
        /// </summary>
        public static bool IsPending(string status)
        {
            return status.Equals(Pending, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra plan bị từ chối không
        /// </summary>
        public static bool IsRejected(string status)
        {
            return status.Equals(Rejected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra HR có thể review (approve/reject) plan không
        /// </summary>
        public static bool CanReview(string status)
        {
            return IsPending(status);
        }

        /// <summary>
        /// Kiểm tra có thể tạo JobPosting không
        /// </summary>
        public static bool CanCreateJobPosting(string status)
        {
            return IsApproved(status);
        }

        /// <summary>
        /// Kiểm tra có thể chuyển sang trạng thái mới không
        /// </summary>
        public static bool CanTransitionTo(string currentStatus, string newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                // HR review
                (Pending, Approved) => true,
                (Pending, Rejected) => true,

                // HR can revoke approval
                (Approved, Rejected) => true,

                _ => false
            };
        }

        /// <summary>
        /// Lấy mô tả trạng thái
        /// </summary>
        public static string GetDescription(string status)
        {
            return status switch
            {
                Pending => "Chờ phê duyệt",
                Approved => "Đã phê duyệt",
                Rejected => "Bị từ chối",
                _ => "Không xác định"
            };
        }
    }
}
