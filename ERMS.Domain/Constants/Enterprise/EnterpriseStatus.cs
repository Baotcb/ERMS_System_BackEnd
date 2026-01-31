using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Domain.Constants.Enterprise
{
    public static class EnterpriseStatus
    {
        public const string Active = "Active"; //•	✅ Active: Doanh nghiệp đang hoạt động bình thường
        public const string Locked = "Locked"; //•	🔒 Locked: Doanh nghiệp bị khóa bởi Admin
        public const string Suspended = "Suspended"; // •	⏸️ Suspended: Doanh nghiệp bị tạm ngưng khi vừa khởi tạo để chờ admin duyệt 
        public const string Inactive = "Inactive";// •	❌ Inactive: Doanh nghiệp không hoạt động

        public static readonly string[] ValidStatuses = { Active, Locked, Suspended, Inactive };

        public static bool IsValid(string status)
        {
            return Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsActive(string status)
        {
            return status.Equals(Active, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLocked(string status)
        {
            return status.Equals(Locked, StringComparison.OrdinalIgnoreCase);
        }
    }

}
