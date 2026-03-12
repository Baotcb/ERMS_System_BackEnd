using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ERMS.Application.Features.Quizzes.Commands.ImportQuizQuestions
{
    public class ImportQuizQuestionsCommandHandler
    : IRequestHandler<ImportQuizQuestionsCommand, int>
    {
        private readonly IERMSDbContext _context;

        public ImportQuizQuestionsCommandHandler(IERMSDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(
            ImportQuizQuestionsCommand request,
            CancellationToken cancellationToken)
        {
            var questions = new List<QuizQuestion>();

            using var stream = new MemoryStream();
            await request.File.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var options = new Dictionary<string, string>
            {
                { "A", worksheet.Cells[row,2].Text },
                { "B", worksheet.Cells[row,3].Text },
                { "C", worksheet.Cells[row,4].Text },
                { "D", worksheet.Cells[row,5].Text }
            };

                var question = new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    QuizId = request.QuizId,
                    QuestionText = worksheet.Cells[row, 1].Text,
                    Options = JsonSerializer.Serialize(options),
                    CorrectAnswer = worksheet.Cells[row, 6].Text,
                    Explanation = worksheet.Cells[row, 7].Text,
                    Points = int.Parse(worksheet.Cells[row, 8].Text),
                    OrderIndex = int.Parse(worksheet.Cells[row, 9].Text),
                    IsActive = true
                };

                questions.Add(question);
            }

            _context.QuizQuestions.AddRange(questions);

            await _context.SaveChangesAsync(cancellationToken);

            return questions.Count;
        }
    }
}
