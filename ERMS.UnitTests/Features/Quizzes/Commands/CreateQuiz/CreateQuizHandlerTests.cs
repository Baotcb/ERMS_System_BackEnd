using ERMS.Application.Features.Quizzes.Commands.CreateQuiz;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Training;
using ERMS.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.CreateQuiz
{
    public class CreateQuizHandlerTests
    {
        private readonly Mock<IERMSDbContext> _contextMock;
        private readonly CreateQuizHandler _handler;

        public CreateQuizHandlerTests()
        {
            _contextMock = new Mock<IERMSDbContext>();
            _handler = new CreateQuizHandler(_contextMock.Object);
        }

        [Fact]
        public async Task Handle_CourseNotFound_ShouldThrowException()
        {
            _contextMock.Setup(x => x.Courses)
                .Returns(new List<Course>().AsQueryable().BuildMockDbSet().Object);

            var command = new CreateQuizCommand
            {
                CourseId = Guid.NewGuid(),
                QuizTitle = "Quiz 1"
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy khóa học");
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateQuiz()
        {
            var courseId = Guid.NewGuid();

            var courses = new List<Course>
            {
                new Course { Id = courseId }
            };

            _contextMock.Setup(x => x.Courses)
                .Returns(courses.AsQueryable().BuildMockDbSet().Object);

            _contextMock.Setup(x => x.Quizzes.Add(It.IsAny<Quiz>()));

            _contextMock.Setup(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var command = new CreateQuizCommand
            {
                CourseId = courseId,
                QuizTitle = "Test Quiz"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeEmpty();

            _contextMock.Verify(x =>
                x.Quizzes.Add(It.IsAny<Quiz>()),
                Times.Once);
        }
    }
}