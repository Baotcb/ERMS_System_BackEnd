using ClosedXML.Excel;
using ERMS.Application.Interface;
using ERMS.Domain.Constants;
using ERMS.Domain.Constants.Roles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ERMS.Infrastructure.Services
{
    /// <summary>
    /// Service để parse file Excel/CSV import nhân viên.
    /// </summary>
    public class ExcelParserService : IExcelParserService
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public ExcelParseResult ParseEmployeeImportFile(Stream fileStream, string fileName)
        {
            var result = new ExcelParseResult();

            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                IXLWorkbook workbook;

                switch (extension)
                {
                    case ".xlsx":
                    case ".xls":
                        workbook = new XLWorkbook(fileStream);
                        break;
                    case ".csv":
                        workbook = LoadCsvAsWorkbook(fileStream);
                        break;
                    default:
                        result.Errors.Add(new ParseError
                        {
                            RowNumber = 0,
                            Column = "File",
                            Message = $"Định dạng file '{extension}' không được hỗ trợ. Chỉ hỗ trợ .xlsx, .xls, .csv"
                        });
                        return result;
                }

                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null || worksheet.Rows().Count() == 0)
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = 0,
                        Column = "Sheet",
                        Message = "File không có dữ liệu"
                    });
                    return result;
                }

                var headerRow = worksheet.FirstRow();
                if (headerRow == null)
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = 0,
                        Column = "Header",
                        Message = "File không có header"
                    });
                    return result;
                }

                var headerMappings = new Dictionary<string, ColumnMapping>(StringComparer.OrdinalIgnoreCase);
                var foundColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var cell in headerRow.Cells())
                {
                    var header = cell.GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    var mappedKey = ColumnAliases.FindColumnKey(header);

                    headerMappings[mappedKey ?? header] = new ColumnMapping
                    {
                        OriginalHeader = header,
                        MappedKey = mappedKey
                    };

                    if (mappedKey != null)
                    {
                        foundColumns.Add(mappedKey);
                        result.ColumnMappings.Add(new ColumnMapping
                        {
                            OriginalHeader = header,
                            MappedKey = mappedKey
                        });
                    }
                    else
                    {
                        result.UnknownColumns.Add(header);
                        result.Warnings.Add(new ParseWarning
                        {
                            Type = "unknown_column",
                            Column = header,
                            Message = $"Cột '{header}' không được nhận diện, sẽ bị bỏ qua"
                        });
                    }
                }

                foreach (var required in ColumnAliases.RequiredColumns)
                {
                    if (foundColumns.Contains(required))
                    {
                        continue;
                    }

                    result.MissingRequiredColumns.Add(required);
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = 0,
                        Column = required,
                        Message = $"Thiếu cột bắt buộc: {required}"
                    });
                }

                var usedRange = worksheet.RangeUsed();
                if (usedRange == null)
                {
                    return result;
                }

                var dataRows = usedRange.Rows().Skip(1);
                var currentRowNumber = 2;

                foreach (var row in dataRows)
                {
                    var parsedRow = ParseDataRow(row, headerMappings, currentRowNumber);
                    result.Rows.Add(parsedRow);

                    if (!parsedRow.IsValid)
                    {
                        result.Errors.AddRange(parsedRow.ParseErrors ?? Enumerable.Empty<ParseError>());
                    }
                    else
                    {
                        result.Warnings.AddRange(parsedRow.ParseWarnings ?? Enumerable.Empty<ParseWarning>());
                    }

                    currentRowNumber++;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    RowNumber = 0,
                    Column = "File",
                    Message = $"Lỗi khi đọc file: {ex.Message}"
                });
                return result;
            }
        }

        private IXLWorkbook LoadCsvAsWorkbook(Stream fileStream)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            using var reader = new StreamReader(fileStream);
            var rowIndex = 1;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = SplitCsvLine(line);
                for (var i = 0; i < values.Count; i++)
                {
                    worksheet.Cell(rowIndex, i + 1).Value = values[i];
                }

                rowIndex++;
            }

            return workbook;
        }

        private List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var currentValue = new System.Text.StringBuilder();
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            result.Add(currentValue.ToString());
            return result;
        }

        private ParsedEmployeeRow ParseDataRow(IXLRangeRow row, Dictionary<string, ColumnMapping> headerMappings, int rowNumber)
        {
            var parsedRow = new ParsedEmployeeRow
            {
                RowNumber = rowNumber,
                ParseErrors = new List<ParseError>(),
                ParseWarnings = new List<ParseWarning>()
            };

            string GetCellValue(string key)
            {
                if (!headerMappings.TryGetValue(key, out var mapping) || mapping.MappedKey == null)
                {
                    return string.Empty;
                }

                var headerCell = row.Worksheet.Row(1).Cells()
                    .FirstOrDefault(c => c.GetString()?.Trim().Equals(mapping.OriginalHeader, StringComparison.OrdinalIgnoreCase) ?? false);

                if (headerCell == null)
                {
                    return string.Empty;
                }

                var cell = row.Cell(headerCell.Address.ColumnNumber);
                return cell.GetString()?.Trim() ?? string.Empty;
            }

            parsedRow.FullName = GetCellValue("FullName");
            parsedRow.Email = GetCellValue("Email");
            parsedRow.Phone = GetCellValue("Phone");
            parsedRow.DepartmentCode = GetCellValue("DepartmentCode");
            parsedRow.Position = GetCellValue("Position");
            parsedRow.Password = GetCellValue("Password");

            var rawRole = GetCellValue("Role");
            if (!string.IsNullOrWhiteSpace(rawRole))
            {
                var normalizedRole = RoleAliases.NormalizeRole(rawRole);
                if (normalizedRole == null)
                {
                    parsedRow.IsValid = false;
                    parsedRow.ParseErrors!.Add(new ParseError
                    {
                        RowNumber = rowNumber,
                        Column = "Role",
                        Value = rawRole,
                        Message = $"Role '{rawRole}' không hợp lệ. Sử dụng: Employee, Trainer, Director, DepartmentHead"
                    });
                }
                else if (!RoleAliases.IsValidForBulkImport(normalizedRole))
                {
                    parsedRow.IsValid = false;
                    parsedRow.ParseErrors!.Add(new ParseError
                    {
                        RowNumber = rowNumber,
                        Column = "Role",
                        Value = rawRole,
                        Message = $"Role '{rawRole}' không được phép import qua file."
                    });
                }
                else
                {
                    parsedRow.Role = normalizedRole;
                }
            }

            var effectiveRole = parsedRow.Role ?? AppRoles.Employee;
            var isDirectorRole = effectiveRole.Equals(AppRoles.Director, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(parsedRow.FullName))
            {
                parsedRow.IsValid = false;
                parsedRow.ParseErrors!.Add(new ParseError
                {
                    RowNumber = rowNumber,
                    Column = "FullName",
                    Message = "Họ và tên là bắt buộc"
                });
            }

            if (string.IsNullOrWhiteSpace(parsedRow.Email))
            {
                parsedRow.IsValid = false;
                parsedRow.ParseErrors!.Add(new ParseError
                {
                    RowNumber = rowNumber,
                    Column = "Email",
                    Message = "Email là bắt buộc"
                });
            }
            else if (!EmailRegex.IsMatch(parsedRow.Email))
            {
                parsedRow.IsValid = false;
                parsedRow.ParseErrors!.Add(new ParseError
                {
                    RowNumber = rowNumber,
                    Column = "Email",
                    Value = parsedRow.Email,
                    Message = "Email không đúng định dạng"
                });
            }

            if (!isDirectorRole && string.IsNullOrWhiteSpace(parsedRow.DepartmentCode))
            {
                parsedRow.IsValid = false;
                parsedRow.ParseErrors!.Add(new ParseError
                {
                    RowNumber = rowNumber,
                    Column = "DepartmentCode",
                    Message = "Mã phòng ban là bắt buộc (trừ Director)"
                });
            }

            if (!parsedRow.ParseErrors!.Any())
            {
                parsedRow.IsValid = true;

                if (string.IsNullOrWhiteSpace(parsedRow.Password))
                {
                    parsedRow.ParseWarnings!.Add(new ParseWarning
                    {
                        Type = "empty_value",
                        Column = "Password",
                        Message = $"Dòng {rowNumber}: Không có mật khẩu, sẽ tự động tạo"
                    });
                }

                if (isDirectorRole && !string.IsNullOrWhiteSpace(parsedRow.DepartmentCode))
                {
                    parsedRow.ParseWarnings!.Add(new ParseWarning
                    {
                        Type = "ignored_value",
                        Column = "DepartmentCode",
                        Message = $"Dòng {rowNumber}: Role Director sẽ bỏ qua Department Code"
                    });
                }
            }

            return parsedRow;
        }
    }
}
