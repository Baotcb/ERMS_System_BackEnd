using ERMS.Application.Features.Quizzes.Commands.CreateQuizQuestion;
using ERMS.Application.Features.Quizzes.Commands.ImportQuizQuestions;
using ERMS.Application.Features.Quizzes.Commands.StartQuiz;
using ERMS.Application.Features.Quizzes.Commands.SubmitAnswer;
using ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;
using ERMS.Application.Features.Quizzes.Queries.GetQuizQuestions;
using ERMS.Application.Features.Quizzes.Queries.GetQuizReview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers
{
    [ApiController]
    [Route("api/quizzes")]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuizController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("{quizId}/start")]
        public async Task<IActionResult> StartQuiz(Guid quizId)
        {
            var result = await _mediator.Send(new StartQuizCommand
            {
                QuizId = quizId
            });

            return Ok(result);
        }

        [HttpPost("{attemptId}/answers")]
        public async Task<IActionResult> SubmitAnswer(
        Guid attemptId,
        [FromBody] SubmitAnswerCommand command)
        {
            command.AttemptId = attemptId;

            await _mediator.Send(command);

            return Ok();
        }

        [HttpPost("attempts/{attemptId}/submit")]
        public async Task<IActionResult> SubmitQuiz(Guid attemptId)
        {
            var result = await _mediator.Send(new SubmitQuizCommand
            {
                AttemptId = attemptId
            });

            return Ok(result);
        }

        [HttpGet("attempts/{attemptId}/review")]
        public async Task<IActionResult> GetQuizReview(Guid attemptId)
        {
            var result = await _mediator.Send(new GetQuizReviewQuery
            {
                AttemptId = attemptId
            });

            return Ok(result);
        }

        [HttpPost("{quizId}/questions")]
        public async Task<IActionResult> CreateQuestion(
    Guid quizId,
    [FromBody] CreateQuizQuestionCommand command)
        {
            command.QuizId = quizId;

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpPost("{quizId}/import-excel")]
        public async Task<IActionResult> ImportQuizQuestions(
    Guid quizId,
    IFormFile file)
        {
            var command = new ImportQuizQuestionsCommand
            {
                QuizId = quizId,
                File = file
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        [HttpGet("{attemptId}/questions")]
        public async Task<IActionResult> GetQuizQuestions(Guid attemptId)
        {
            var result = await _mediator.Send(new GetQuizQuestionsQuery
            {
                AttemptId = attemptId
            });

            return Ok(result);
        }
    }
}
