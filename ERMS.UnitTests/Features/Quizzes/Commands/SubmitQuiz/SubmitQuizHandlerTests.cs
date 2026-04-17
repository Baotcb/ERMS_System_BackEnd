using ERMS.Application.Features.Quizzes.Commands.SubmitQuiz;
using ERMS.Domain.Entities.Organization;
using ERMS.Domain.Entities.Training;
using ERMS.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Quizzes.Commands.SubmitQuiz
{
    public class SubmitQuizHandlerTests : IDisposable
    {
        private readonly ERMSDbContext _context;
        private readonly SubmitQuizHandler _handler;

        public SubmitQuizHandlerTests()
        {
            var options = new DbContextOptionsBuilder<ERMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ERMSDbContext(options);
            _handler = new SubmitQuizHandler(_context);
        }

        [Fact]
        public async Task Handle_AttemptNotFound_ShouldThrowException()
        {
            var command = new SubmitQuizCommand
            {
                AttemptId = Guid.NewGuid()
            };

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should()
                .ThrowAsync<Exception>()
                .WithMessage("Không tìm thấy lượt làm bài");
        }

        [Fact]
        public async Task Handle_AllAnswersCorrect_ShouldReturnFullScore()
        {
            var attemptId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();

            var q1 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "A", Points = 1, QuestionText = "1", Options = "1" };
            var q2 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "B", Points = 1, QuestionText = "2", Options = "2" };

            var course = new Course
            {
                Id = courseId,
                CourseName = "Course",
                CourseCode = "C01",
                TrainerEmail = "test@mail.com"
            };

            var employee = new Employee
            {
                Id = employeeId,
                UserId = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                EmployeeCode = "E001"
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                CourseId = courseId,
                EmployeeId = employeeId,
                Status = "InProgress"
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = enrollmentId,
                TotalQuestions = 2,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { q1, q2 },
                    QuizTitle = "Sample Quiz"
                },
                QuizAnswers = new List<QuizAnswer>
        {
            new QuizAnswer { QuizQuestionId = q1.Id, SelectedAnswer = "A" },
            new QuizAnswer { QuizQuestionId = q2.Id, SelectedAnswer = "B" }
        }
            };

            _context.Courses.Add(course);
            _context.Employees.Add(employee);
            _context.Enrollments.Add(enrollment);
            _context.QuizAttempts.Add(attempt);

            await _context.SaveChangesAsync();

            var result = await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            result.Score.Should().Be(100);
            result.CorrectAnswers.Should().Be(2);
            result.TotalQuestions.Should().Be(2);
            result.IsPassed.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PartialCorrect_ShouldCalculateScoreCorrectly()
        {
            var attemptId = Guid.NewGuid();

            var q1 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "A", Points = 1, QuestionText = "1", Options = "1" };
            var q2 = new QuizQuestion { Id = Guid.NewGuid(), CorrectAnswer = "B", Points = 1, QuestionText = "2", Options = "2" };


            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = Guid.NewGuid(),
                TotalQuestions = 2,
                Quiz = new Quiz
                {
                    PassingScore = 70,
                    Questions = new List<QuizQuestion> { q1, q2 },
                    QuizTitle = "Sample Quiz"
                },
                QuizAnswers = new List<QuizAnswer>
                {
                    new QuizAnswer { QuizQuestionId = q1.Id, SelectedAnswer = "A" },
                    new QuizAnswer { QuizQuestionId = q2.Id, SelectedAnswer = "C" }
                }
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            result.Score.Should().Be(50);
            result.CorrectAnswers.Should().Be(1);
            result.IsPassed.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldUpdateEnrollment_WhenPassed()
        {
            var attemptId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1,
                Options = "A",
                QuestionText = "Sample Question"
            };

            var course = new Course
            {
                Id = courseId,
                CourseName = "Course",
                CourseCode = "C01",
                TrainerEmail = "test@mail.com"
            };

            var employee = new Employee
            {
                Id = employeeId,
                UserId = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                EmployeeCode = "E001"
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                CourseId = courseId,
                EmployeeId = employeeId,
                Status = "InProgress"
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = enrollmentId,
                TotalQuestions = 1,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question },
                    QuizTitle = "Sample Quiz"
                },
                QuizAnswers = new List<QuizAnswer>
        {
            new QuizAnswer
            {
                QuizQuestionId = question.Id,
                SelectedAnswer = "A"
            }
        }
            };

            _context.Courses.Add(course);
            _context.Employees.Add(employee);
            _context.Enrollments.Add(enrollment);
            _context.QuizAttempts.Add(attempt);

            await _context.SaveChangesAsync();

            await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            enrollment.Status.Should().Be("Completed");
            enrollment.CompletedAt.Should().NotBeNull();
        }
        [Fact]
        public async Task Handle_PassedQuiz_ShouldAddSkillsToEmployee()
        {
            var attemptId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var skillId = Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1,
                Options = "A",
                QuestionText = "Sample Question"
            };

            var skill = new ERMS.Domain.Entities.Skill.Skill
            {
                Id = skillId,
                SkillName = "C#"
            };

            var course = new Course
            {
                Id = courseId,
                CourseName = "Course",
                CourseCode = "C01",
                TrainerEmail = "test@mail.com"
            };

            var courseSkill = new CourseSkill
            {
                CourseId = courseId,
                SkillId = skillId,
                Skill = skill
            };

            var employee = new Employee
            {
                Id = employeeId,
                UserId = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                EmployeeCode = "E001",
                SkillDescription = ""
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                CourseId = courseId,
                EmployeeId = employeeId,
                Status = "InProgress"
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = enrollmentId,
                TotalQuestions = 1,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question },
                    QuizTitle = "Quiz"
                },
                QuizAnswers = new List<QuizAnswer>
        {
            new QuizAnswer
            {
                QuizQuestionId = question.Id,
                SelectedAnswer = "A"
            }
        }
            };

            _context.Skills.Add(skill);
            _context.Courses.Add(course);
            _context.CourseSkills.Add(courseSkill);
            _context.Employees.Add(employee);
            _context.Enrollments.Add(enrollment);
            _context.QuizAttempts.Add(attempt);

            await _context.SaveChangesAsync();

            await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            var updatedEmployee = await _context.Employees.FindAsync(employeeId);

            updatedEmployee!.SkillDescription.Should().Contain("C#");
        }

        [Fact]
        public async Task Handle_ShouldSaveChanges()
        {
            var attemptId = Guid.NewGuid();
            var enrollmentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();

            var question = new QuizQuestion
            {
                Id = Guid.NewGuid(),
                CorrectAnswer = "A",
                Points = 1,
                Options = "A",
                QuestionText = "Sample Question"
            };

            var course = new Course
            {
                Id = courseId,
                CourseName = "Course",
                CourseCode = "C01",
                TrainerEmail = "test@mail.com"
            };

            var employee = new Employee
            {
                Id = employeeId,
                UserId = Guid.NewGuid(),
                EnterpriseId = Guid.NewGuid(),
                EmployeeCode = "E001"
            };

            var enrollment = new Enrollment
            {
                Id = enrollmentId,
                CourseId = courseId,
                EmployeeId = employeeId,
                Status = "InProgress"
            };

            var attempt = new QuizAttempt
            {
                Id = attemptId,
                EnrollmentId = enrollmentId,
                TotalQuestions = 1,
                Quiz = new Quiz
                {
                    PassingScore = 50,
                    Questions = new List<QuizQuestion> { question },
                    QuizTitle = "Sample Quiz"
                },
                QuizAnswers = new List<QuizAnswer>
        {
            new QuizAnswer
            {
                QuizQuestionId = question.Id,
                SelectedAnswer = "A"
            }
        }
            };

            _context.Courses.Add(course);
            _context.Employees.Add(employee);
            _context.Enrollments.Add(enrollment);
            _context.QuizAttempts.Add(attempt);

            await _context.SaveChangesAsync();

            await _handler.Handle(
                new SubmitQuizCommand { AttemptId = attemptId },
                CancellationToken.None);

            var updated = await _context.QuizAttempts.FindAsync(attemptId);
            updated!.Status.Should().Be("Completed");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}