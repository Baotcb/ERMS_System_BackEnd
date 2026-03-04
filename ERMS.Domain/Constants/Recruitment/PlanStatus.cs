using System;

namespace ERMS.Domain.Constants.Recruitment
{
    /// <summary>
    /// Trạng thái của Kế hoạch Tuyển dụng (RecruitmentPlan)
    /// </summary>
    public static class PlanStatus
    {
        /// <summary>
        /// 📝 Draft: Dept Head đang soạn thảo, chưa submit
        /// </summary>
        public const string Draft = "Draft";

        /// <summary>
        /// ⏳ Pending: Dept Head đã submit, đang chờ Director phê duyệt
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// ✅ Approved: Director đã phê duyệt, có thể tạo JobPosting
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// ❌ Rejected: Director từ chối, cần chỉnh sửa và resubmit
        /// </summary>
        public const string Rejected = "Rejected";

        public static readonly string[] ValidStatuses = { Draft, Pending, Approved, Rejected };

        /// <summary>
        /// Kiểm tra trạng thái có hợp lệ không
        /// </summary>
        public static bool IsValid(string status)
        {
            return Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kiểm tra plan đang ở Draft
        /// </summary>
        public static bool IsDraft(string status)
        {
            return status.Equals(Draft, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra plan đang chờ phê duyệt
        /// </summary>
        public static bool IsPending(string status)
        {
            return status.Equals(Pending, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra plan đã được phê duyệt
        /// </summary>
        public static bool IsApproved(string status)
        {
            return status.Equals(Approved, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra plan bị từ chối
        /// </summary>
        public static bool IsRejected(string status)
        {
            return status.Equals(Rejected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra có thể CRUD PlanDetails không (Draft OR Rejected)
        /// </summary>
        public static bool CanEditPlanDetails(string status)
        {
            return IsDraft(status) || IsRejected(status);
        }

        /// <summary>
        /// Kiểm tra có thể Submit không (Draft only)
        /// </summary>
        public static bool CanSubmit(string status)
        {
            return IsDraft(status);
        }

        /// <summary>
        /// Kiểm tra có thể Resubmit không (Rejected only)
        /// </summary>
        public static bool CanResubmit(string status)
        {
            return IsRejected(status);
        }

        /// <summary>
        /// Kiểm tra Director có thể review (approve/reject) không
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
                // Dept Head submit
                (Draft, Pending) => true,

                // Dept Head resubmit
                (Rejected, Pending) => true,

                // Director review
                (Pending, Approved) => true,
                (Pending, Rejected) => true,

                // Director can revoke approval
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
                Draft => "Đang soạn thảo",
                Pending => "Chờ phê duyệt",
                Approved => "Đã phê duyệt",
                Rejected => "Bị từ chối",
                _ => "Không xác định"
            };
        }
    }
}
