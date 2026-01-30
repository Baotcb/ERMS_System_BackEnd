using System;
using System.Collections.Generic;
using System.Linq;

namespace ERMS.Domain.Constants.Roles
{
    /// <summary>
    /// Danh sách alias cho các roles trong hệ thống
    /// Hỗ trợ nhận diện role bằng tiếng Việt và tiếng Anh
    /// </summary>
    public static class RoleAliases
    {
        public static readonly Dictionary<string, string> Mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Employee
            ["employee"] = AppRoles.Employee,
            ["nhân viên"] = AppRoles.Employee,
            ["nhan vien"] = AppRoles.Employee,
            ["nv"] = AppRoles.Employee,

            // Trainer
            ["trainer"] = AppRoles.Trainer,
            ["đào tạo"] = AppRoles.Trainer,
            ["giảng viên"] = AppRoles.Trainer,
            ["giao vien"] = AppRoles.Trainer,

            // Director
            ["director"] = AppRoles.Director,
            ["giám đốc"] = AppRoles.Director,
            ["giam doc"] = AppRoles.Director,

            // DepartmentHead
            ["departmenthead"] = AppRoles.DepartmentHead,
            ["department head"] = AppRoles.DepartmentHead,
            ["trưởng phòng"] = AppRoles.DepartmentHead,
            ["truong phong"] = AppRoles.DepartmentHead,
            ["tp"] = AppRoles.DepartmentHead,
        };

        /// <summary>
        /// Chuẩn hóa tên role về role chuẩn của hệ thống
        /// </summary>
        public static string? NormalizeRole(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return Mappings.TryGetValue(input.Trim(), out var role) ? role : null;
        }

        /// <summary>
        /// Kiểm tra xem role có hợp lệ cho bulk import không
        /// (Không cho phép import Admin, HRManager, Candidate qua bulk import vì các role này nhạy cảm)
        /// </summary>
        public static bool IsValidForBulkImport(string role)
        {
            return ValidRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Danh sách các role được phép import qua bulk import
        /// </summary>
        public static readonly string[] ValidRoles = {
            AppRoles.Employee,
            AppRoles.Trainer,
            AppRoles.Director,
            AppRoles.DepartmentHead
        };

        /// <summary>
        /// Lấy danh sách các alias cho một role (để hiển thị error message chi tiết)
        /// </summary>
        public static string[] GetAliasesForRole(string role)
        {
            return Mappings
                .Where(kvp => kvp.Value.Equals(role, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToArray();
        }
    }
}
