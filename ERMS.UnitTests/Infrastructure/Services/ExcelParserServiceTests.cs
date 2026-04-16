using System.IO;
using ClosedXML.Excel;
using ERMS.Infrastructure.Services;
using FluentAssertions;

namespace ERMS.UnitTests.Infrastructure.Services;

public class ExcelParserServiceTests
{
    [Fact]
    public void ParseEmployeeImportFile_ShouldMapSkillDescriptionAlias()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Employees");
            sheet.Cell(1, 1).Value = "Full Name";
            sheet.Cell(1, 2).Value = "Email";
            sheet.Cell(1, 3).Value = "Skill Description";
            sheet.Cell(1, 4).Value = "Department Code";
            sheet.Cell(2, 1).Value = "Nguyen Van A";
            sheet.Cell(2, 2).Value = "a@example.com";
            sheet.Cell(2, 3).Value = "C#/.NET";
            sheet.Cell(2, 4).Value = "IT";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var service = new ExcelParserService();

        // Act
        var result = service.ParseEmployeeImportFile(stream, "employees.xlsx");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows.Should().ContainSingle();
        result.Rows[0].SkillDescription.Should().Be("C#/.NET");
        result.ColumnMappings.Should().Contain(m => m.MappedKey == "SkillDescription");
    }
}
