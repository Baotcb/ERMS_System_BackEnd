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
using System.Text;
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

            // data
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
            formFileMock.Setup(f => f.FileName).Returns("test.xlsx");

            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns((Stream s, CancellationToken _) =>
                {
                    stream.Position = 0;
                    stream.CopyTo(s);
                    return Task.CompletedTask;
                });

            var quizQuestions = new List<QuizQuestion>();
            var dbSetMock = quizQuestions.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.QuizQuestions).Returns(dbSetMock.Object);

            _contextMock.Setup(x => x.QuizQuestions.AddRange(It.IsAny<IEnumerable<QuizQuestion>>()))
                .Callback<IEnumerable<QuizQuestion>>(q => quizQuestions.AddRange(q));

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new ImportQuizQuestionsCommand
            {
                QuizId = quizId,
                File = formFileMock.Object
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);
            quizQuestions.Should().HaveCount(1);
            quizQuestions[0].QuestionText.Should().Be("What is C#?");
        }

        [Fact]
        public async Task Handle_ShouldImportQuestionsFromCsv()
        {
            // Arrange
            var quizId = Guid.NewGuid();

            var csvContent =
                "Question,A,B,C,D,Correct,Explanation,Points,Order\n" +
                "What is .NET?,Platform,Language,OS,Database,A,.NET platform,5,1";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("test.csv");

            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns((Stream s, CancellationToken _) =>
                {
                    stream.Position = 0;
                    stream.CopyTo(s);
                    return Task.CompletedTask;
                });

            var quizQuestions = new List<QuizQuestion>();
            var dbSetMock = quizQuestions.AsQueryable().BuildMockDbSet();

            _contextMock.Setup(x => x.QuizQuestions).Returns(dbSetMock.Object);

            _contextMock.Setup(x => x.QuizQuestions.AddRange(It.IsAny<IEnumerable<QuizQuestion>>()))
                .Callback<IEnumerable<QuizQuestion>>(q => quizQuestions.AddRange(q));

            _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new ImportQuizQuestionsCommand
            {
                QuizId = quizId,
                File = formFileMock.Object
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);
            quizQuestions.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenFileFormatInvalid()
        {
            // Arrange
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("test.txt");

            var command = new ImportQuizQuestionsCommand
            {
                QuizId = Guid.NewGuid(),
                File = formFileMock.Object
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Unsupported file format*");
        }

        [Fact]
        public async Task Handle_ShouldReturnZero_WhenNoValidQuestions()
        {
            // Arrange
            ExcelPackage.License.SetNonCommercialPersonal("UnitTest");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // header only
            worksheet.Cells[1, 1].Value = "Question";

            var stream = new MemoryStream(package.GetAsByteArray());

            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("test.xlsx");

            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns((Stream s, CancellationToken _) =>
                {
                    stream.Position = 0;
                    stream.CopyTo(s);
                    return Task.CompletedTask;
                });

            var quizQuestions = new List<QuizQuestion>();

            _contextMock.Setup(x => x.QuizQuestions)
                .Returns(quizQuestions.AsQueryable().BuildMockDbSet().Object);

            var command = new ImportQuizQuestionsCommand
            {
                QuizId = Guid.NewGuid(),
                File = formFileMock.Object
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }
    }
}