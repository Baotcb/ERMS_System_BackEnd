using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Application.Features.Quizzes.Commands.ImportQuizQuestions
{
    public class ImportQuizQuestionsCommand : IRequest<int>
    {
        public Guid QuizId { get; set; }

        public IFormFile File { get; set; } = null!;
    }
}
