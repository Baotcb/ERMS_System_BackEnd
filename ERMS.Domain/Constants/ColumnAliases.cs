using System.Collections.Generic;
using System.Linq;

namespace ERMS.Domain.Constants
{
    /// <summary>
    /// Danh sách alias cho các cột trong file Excel import
    /// Hỗ trợ nhận diện tên cột bằng tiếng Việt (có dấu, không dấu) và tiếng Anh
    /// </summary>
    public static class ColumnAliases
    {
        public static readonly Dictionary<string, string[]> Mappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullName"] = new[] {
                "full name", "fullname", "name", "họ và tên", "ho va ten",
                "họ tên", "ho ten", "tên nhân viên", "ten nhan vien",
                "tên", "ten"
            },
            ["Email"] = new[] {
                "email", "e-mail", "mail", "địa chỉ email", "dia chi email"
            },
            ["Phone"] = new[] {
                "phone", "phone number", "mobile", "số điện thoại",
                "so dien thoai", "sđt", "sdt", "điện thoại", "dien thoai"
            },
            ["DepartmentCode"] = new[] {
                "department code", "departmentcode", "dept code",
                "mã phòng ban", "ma phong ban", "phòng ban", "phong ban",
                "mã pb", "ma pb", "mã phòng", "ma phong"
            },
            ["Position"] = new[] {
                "position", "job title", "title", "chức vụ",
                "chuc vu", "vị trí", "vi tri", "chuc danh"
            },
            ["Password"] = new[] {
                "password", "pass", "mật khẩu", "mat khau"
            },
            ["Role"] = new[] {
                "role", "vai trò", "vai tro", "quyền", "quyen", "chức danh"
            }
        };

        /// <summary>
        /// Tìm key chuẩn từ header cột trong file Excel
        /// </summary>
        public static string? FindColumnKey(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return null;

            var normalized = header.Trim().ToLowerInvariant();

            foreach (var kvp in Mappings)
            {
                if (kvp.Value.Any(alias => alias.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                    return kvp.Key;
            }

            return null;
        }

        /// <summary>
        /// Danh sách các cột bắt buộc phải có trong file import
        /// </summary>
        public static readonly string[] RequiredColumns = { "FullName", "Email", "DepartmentCode" };

        /// <summary>
        /// Kiểm tra xem header có phải là cột bắt buộc không
        /// </summary>
        public static bool IsRequiredColumn(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return RequiredColumns.Contains(key, StringComparer.OrdinalIgnoreCase);
        }
    }
}
