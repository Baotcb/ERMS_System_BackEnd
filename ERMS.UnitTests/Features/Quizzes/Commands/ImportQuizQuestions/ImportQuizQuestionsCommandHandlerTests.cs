using ERMS.Application.Features.Quizzes.Commands.ImportQuizQuestions;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.ImportQuizQuestions
{
    public class ImportQuizQuestionsCommandHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock = new();

        private readonly ImportQuizQuestionsCommandHandler _handler;

        public ImportQuizQuestionsCommandHandlerTests()
        {
            _handler = new ImportQuizQuestionsCommandHandler(_contextMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldImportQuestionsFromExcel()
        {
            // Arrange
            var quizId = Guid.NewGuid();
            // Add this early in your application startup, before any Excel operations
            // Set license for EPPlus 8+
            ExcelPackage.License.SetNonCommercialPersonal("UnitTest");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // header
            worksheet.Cells[1, 1].Value = "Question";
            worksheet.Cells[1, 2].Value = "A";
            worksheet.Cells[1, 3].Value = "B";
            worksheet.Cells[1, 4].Value = "C";
            worksheet.Cells[1, 5].Value = "D";
            worksheet.Cells[1, 6].Value = "Correct";
            worksheet.Cells[1, 7].Value = "Explanation";
            worksheet.Cells[1, 8].Value = "Points";
            worksheet.Cells[1, 9].Value = "Order";

            // row 1
            worksheet.Cells[2, 1].Value = "What is C#?";
            worksheet.Cells[2, 2].Value = "Language";
            worksheet.Cells[2, 3].Value = "Framework";
            worksheet.Cells[2, 4].Value = "OS";
            worksheet.Cells[2, 5].Value = "Database";
            worksheet.Cells[2, 6].Value = "A";
            worksheet.Cells[2, 7].Value = "C# is a programming language";
            worksheet.Cells[2, 8].Value = "10";
            worksheet.Cells[2, 9].Value = "1";

            var stream = new MemoryStream(package.GetAsByteArray());

            var formFileMock = new Mock<IFormFile>();

            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns((Stream s, CancellationToken _) =>
                {
                    stream.CopyTo(s);
                    return Task.CompletedTask;
                });

            var quizQuestions = new List<QuizQuestion>();

            _contextMock.Setup(x => x.QuizQuestions)
                .Returns(quizQuestions.AsQueryable().BuildMockDbSet().Object);

            var command = new ImportQuizQuestionsCommand
            {
                QuizId = quizId,
                File = formFileMock.Object
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);
        }
    }
}