using System;
using System.Collections.Generic;
using System.IO;

namespace ERMS.Application.Interface
{
    /// <summary>
    /// Service để parse file Excel/CSV import nhân viên
    /// </summary>
    public interface IExcelParserService
    {
        ExcelParseResult ParseEmployeeImportFile(Stream fileStream, string fileName);
    }

    /// <summary>
    /// Kết quả parse file Excel
    /// </summary>
    public class ExcelParseResult
    {
        /// <summary>
        /// Danh sách các dòng đã parse
        /// </summary>
        public List<ParsedEmployeeRow> Rows { get; set; } = new List<ParsedEmployeeRow>();

        /// <summary>
        /// Mapping từ header gốc sang key chuẩn
        /// </summary>
        public List<ColumnMapping> ColumnMappings { get; set; } = new List<ColumnMapping>();

        /// <summary>
        /// Danh sách các cột không được nhận diện
        /// </summary>
        public List<string> UnknownColumns { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách các cột bắt buộc bị thiếu
        /// </summary>
        public List<string> MissingRequiredColumns { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách các lỗi parse
        /// </summary>
        public List<ParseError> Errors { get; set; } = new List<ParseError>();

        /// <summary>
        /// Danh sách các cảnh báo (warning)
        /// </summary>
        public List<ParseWarning> Warnings { get; set; } = new List<ParseWarning>();

        /// <summary>
        /// Kiểm tra kết quả parse có hợp lệ không
        /// </summary>
        public bool IsValid => !Errors.Any() && !MissingRequiredColumns.Any();
    }

    /// <summary>
    /// Thông tin một dòng nhân viên đã parse
    /// </summary>
    public class ParsedEmployeeRow
    {
        /// <summary>
        /// Số dòng trong file Excel (bắt đầu từ 2 vì dòng 1 là header)
        /// </summary>
        public int RowNumber { get; set; }

        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DepartmentCode { get; set; }
        public string? Position { get; set; }
        public string? SkillDescription { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }

        /// <summary>
        /// Dòng này có hợp lệ không (không có lỗi parse)
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Danh sách lỗi parse của dòng này (chỉ dùng trong quá trình parse)
        /// </summary>
        public List<ParseError>? ParseErrors { get; set; }

        /// <summary>
        /// Danh sách cảnh báo của dòng này (chỉ dùng trong quá trình parse)
        /// </summary>
        public List<ParseWarning>? ParseWarnings { get; set; }
    }

    /// <summary>
    /// Mapping của một cột
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>
        /// Header gốc trong file
        /// </summary>
        public string OriginalHeader { get; set; } = string.Empty;

        /// <summary>
        /// Key chuẩn (null nếu không được nhận diện)
        /// </summary>
        public string? MappedKey { get; set; }
    }

    /// <summary>
    /// Lỗi parse ở một dòng
    /// </summary>
    public class ParseError
    {
        /// <summary>
        /// Số dòng có lỗi
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Tên cột có lỗi
        /// </summary>
        public string Column { get; set; } = string.Empty;

        /// <summary>
        /// Giá trị gây lỗi
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Thông báo lỗi
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cảnh báo parse
    /// </summary>
    public class ParseWarning
    {
        /// <summary>
        /// Loại cảnh báo: unknown_column, empty_value
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Tên cột liên quan
        /// </summary>
        public string Column { get; set; } = string.Empty;

        /// <summary>
        /// Thông báo cảnh báo
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
