using System;
using System.Collections.Generic;

namespace ERMS.Application.Features.Employees.Commands.ImportEmployeesFromFile
{
    /// <summary>
    /// Kết quả import nhân viên từ file
    /// </summary>
    public sealed class ImportEmployeesFromFileResult
    {
        // Parse info
        /// <summary>
        /// Mapping từ header gốc sang key chuẩn
        /// </summary>
        public List<ColumnMappingResult> ColumnMappings { get; set; } = new List<ColumnMappingResult>();

        /// <summary>
        /// Danh sách các cột không được nhận diện
        /// </summary>
        public List<string> UnknownColumns { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách các cảnh báo (warning)
        /// </summary>
        public List<ParseWarningResult> Warnings { get; set; } = new List<ParseWarningResult>();

        // Import result
        /// <summary>
        /// Tổng số dòng trong file
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// Số dòng hợp lệ sau khi parse
        /// </summary>
        public int ValidRows { get; set; }

        /// <summary>
        /// Số nhân viên import thành công
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Số nhân viên import thất bại
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Danh sách các lỗi chi tiết
        /// </summary>
        public List<ImportError> Errors { get; set; } = new List<ImportError>();
    }

    /// <summary>
    /// Mapping của một cột
    /// </summary>
    public sealed class ColumnMappingResult
    {
        public string OriginalHeader { get; set; } = string.Empty;
        public string? MappedKey { get; set; }
    }

    /// <summary>
    /// Cảnh báo parse
    /// </summary>
    public sealed class ParseWarningResult
    {
        public string Type { get; set; } = string.Empty; // "unknown_column", "empty_value"
        public string Column { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lỗi import
    /// </summary>
    public sealed class ImportError
    {
        public int RowNumber { get; set; }
        public string? Email { get; set; }
        public string Column { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
