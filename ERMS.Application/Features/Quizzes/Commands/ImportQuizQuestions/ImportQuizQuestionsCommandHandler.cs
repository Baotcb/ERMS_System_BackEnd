using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using MediatR;
using OfficeOpenXml;
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
            stream.Position = 0;

            var fileName = request.File.FileName.ToLower();

            if (fileName.EndsWith(".xlsx"))
            {
                questions = ImportFromExcel(stream, request.QuizId);
            }
            else if (fileName.EndsWith(".csv"))
            {
                questions = ImportFromCsv(stream, request.QuizId);
            }
            else
            {
                throw new Exception("Unsupported file format. Please upload .xlsx or .csv file.");
            }

            if (!questions.Any())
                return 0;

            _context.QuizQuestions.AddRange(questions);
            await _context.SaveChangesAsync(cancellationToken);

            return questions.Count;
        }

        private List<QuizQuestion> ImportFromExcel(Stream stream, Guid quizId)
        {
            var questions = new List<QuizQuestion>();

            ExcelPackage.License.SetNonCommercialPersonal("ERMS");

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];

            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var questionText = worksheet.Cells[row, 1].Text;

                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

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
                    QuizId = quizId,
                    QuestionText = questionText,
                    Options = JsonSerializer.Serialize(options),
                    CorrectAnswer = worksheet.Cells[row, 6].Text,
                    Explanation = worksheet.Cells[row, 7].Text,
                    Points = ParseInt(worksheet.Cells[row, 8].Text),
                    OrderIndex = ParseInt(worksheet.Cells[row, 9].Text),
                    IsActive = true
                };

                questions.Add(question);
            }

            return questions;
        }

        private List<QuizQuestion> ImportFromCsv(Stream stream, Guid quizId)
        {
            var questions = new List<QuizQuestion>();

            using var reader = new StreamReader(stream);

            var header = reader.ReadLine();

            int rowIndex = 1;

            while (!reader.EndOfStream)
            {
                rowIndex++;

                var line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = line.Split(',');

                if (columns.Length < 9)
                    continue;

                var options = new Dictionary<string, string>
                {
                    { "A", columns[1] },
                    { "B", columns[2] },
                    { "C", columns[3] },
                    { "D", columns[4] }
                };

                var question = new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    QuizId = quizId,
                    QuestionText = columns[0],
                    Options = JsonSerializer.Serialize(options),
                    CorrectAnswer = columns[5],
                    Explanation = columns[6],
                    Points = ParseInt(columns[7]),
                    OrderIndex = ParseInt(columns[8]),
                    IsActive = true
                };

                questions.Add(question);
            }

            return questions;
        }

        private int ParseInt(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }
    }
}