using System;

namespace ERMS.Domain.Constants.Recruitment
{
    /// <summary>
    /// Trạng thái của Chi tiết Kế hoạch (PlanDetail)
    /// </summary>
    public static class PlanDetailStatus
    {
        /// <summary>
        /// ⏳ Pending: Chờ Plan được approve
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// ✅ Approved: Plan đã approve, có thể tạo JobPosting
        /// </summary>
        public const string Approved = "Approved";

        /// <summary>
        /// ❌ Rejected: Plan bị reject
        /// </summary>
        public const string Rejected = "Rejected";

        /// <summary>
        /// 📢 Recruiting: Đang tuyển dụng (có JobPosting active)
        /// </summary>
        public const string Recruiting = "Recruiting";

        /// <summary>
        /// 🎯 Fulfilled: Đã tuyển đủ số lượng
        /// </summary>
        public const string Fulfilled = "Fulfilled";

        public static readonly string[] ValidStatuses = { Pending, Approved, Rejected, Recruiting, Fulfilled };

        public static bool IsValid(string status)
        {
            return Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsPending(string status)
        {
            return status.Equals(Pending, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsApproved(string status)
        {
            return status.Equals(Approved, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRejected(string status)
        {
            return status.Equals(Rejected, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRecruiting(string status)
        {
            return status.Equals(Recruiting, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFulfilled(string status)
        {
            return status.Equals(Fulfilled, StringComparison.OrdinalIgnoreCase);
        }
    }
}
