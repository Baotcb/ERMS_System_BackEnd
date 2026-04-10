using System;

namespace ERMS.Domain.Constants.Recruitment
{
    /// <summary>
    /// Trạng thái của Chiến dịch Tuyển dụng (RecruitmentCampaign)
    /// </summary>
    public static class CampaignStatus
    {
        /// <summary>
        /// 📝 Draft: HR đang cấu hình, chưa hiển thị cho Dept Heads
        /// </summary>
        public const string Draft = "Draft";

        /// <summary>
        ///   Open: Đang nhận đề xuất kế hoạch, đang tuyển dụng
        /// </summary>
        public const string Open = "Open";

        /// <summary>
        /// 🔒 Closed: Ngừng nhận đề xuất mới, chỉ xử lý kế hoạch hiện có
        /// </summary>
        public const string Closed = "Closed";

        /// <summary>
        /// 📦 Archived: Dữ liệu lịch sử, chỉ đọc
        /// </summary>
        public const string Archived = "Archived";

        public static readonly string[] ValidStatuses = { Draft, Open, Closed, Archived };

        /// <summary>
        /// Kiểm tra trạng thái có hợp lệ không
        /// </summary>
        public static bool IsValid(string status)
        {
            return Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Kiểm tra có thể submit kế hoạch không
        /// </summary>
        public static bool CanSubmitPlans(string status)
        {
            return status.Equals(Open, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra có thể phê duyệt kế hoạch không
        /// </summary>
        public static bool CanApprovePlans(string status)
        {
            return status.Equals(Open, StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(Closed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra Dept Heads có thể xem không
        /// </summary>
        public static bool IsVisibleToDeptHeads(string status)
        {
            return status.Equals(Open, StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(Closed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra có phải trạng thái chỉ đọc không
        /// </summary>
        public static bool IsReadOnly(string status)
        {
            return status.Equals(Closed, StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(Archived, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra có thể chuyển sang trạng thái mới không
        /// </summary>
        public static bool CanTransitionTo(string currentStatus, string newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                (Draft, Open) => true,      // Draft → Open
                (Open, Closed) => true,     // Open → Closed
                (Closed, Archived) => true, // Closed → Archived
                (Closed, Open) => true,     // Closed → Open (reopen)
                _ => false
            };
        }
    }
}
