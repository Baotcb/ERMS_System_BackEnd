using ERMS.Application.Interface;
using ERMS.Domain.Entities.Application;
using ERMS.Infrastructure.Configuration;
using ERMS.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace ERMS.UnitTests.Infrastructure.Services;

public class CvScoringBackgroundServiceTests
{
    private readonly BackgroundTaskQueue _queue;
    private readonly Mock<IERMSDbContext> _contextMock;
    private readonly Mock<IAIService> _aiServiceMock;
    private readonly Mock<ILogger<CvScoringBackgroundService>> _loggerMock;
    private readonly CvScoringBackgroundService _service;

    public CvScoringBackgroundServiceTests()
    {
        _queue = new BackgroundTaskQueue();
        _contextMock = new Mock<IERMSDbContext>();
        _aiServiceMock = new Mock<IAIService>();
        _loggerMock = new Mock<ILogger<CvScoringBackgroundService>>();

        var scopeMock = new Mock<IServiceScope>();
        var providerMock = new Mock<IServiceProvider>();

        providerMock.Setup(p => p.GetService(typeof(IERMSDbContext))).Returns(_contextMock.Object);
        providerMock.Setup(p => p.GetService(typeof(IAIService))).Returns(_aiServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        _service = new CvScoringBackgroundService(
            _queue,
            scopeFactoryMock.Object,
            _loggerMock.Object,
            Options.Create(new GroqModelSettings { CvScoring = "qwen-3-32b" }));
    }

    private CvScoringWorkItem CreateWorkItem(Guid? applicationId = null)
    {
        return new CvScoringWorkItem(
            ApplicationId: applicationId ?? Guid.NewGuid(),
            ResumeText: "John Doe\nSoftware Engineer\n5 years experience",
            JobDescription: "Looking for .NET developer",
            RequiredSkills: "[\"C#\", \".NET\"]",
            EducationLevel: "Bachelor",
            ExperienceLevel: "Senior"
        );
    }

    private CVScreeningResultDto CreateAIResult()
    {
        return new CVScreeningResultDto
        {
            OverallScore = 85,
            SkillMatchScore = 90,
            ExperienceMatchScore = 80,
            EducationMatchScore = 75,
            KeywordMatchScore = 88,
            MatchedSkills = ["C#", ".NET"],
            MissingSkills = ["Azure"],
            Strengths = ["Strong programming background"],
            Concerns = [],
            Summary = "Good candidate fit.",
            RawResponse = "{}"
        };
    }

    [Fact]
    public async Task BackgroundTaskQueue_ShouldEnqueueAndDequeue()
    {
        // Arrange
        var item = CreateWorkItem();

        // Act
        await _queue.EnqueueAsync(item);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var dequeued = await _queue.DequeueAsync(cts.Token);

        // Assert
        dequeued.Should().Be(item);
        dequeued.ApplicationId.Should().Be(item.ApplicationId);
        dequeued.ResumeText.Should().Be(item.ResumeText);
    }

    [Fact]
    public async Task Service_ShouldProcessWorkItem_AndSaveCVScreeningResult()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var workItem = CreateWorkItem(appId);
        var aiResult = CreateAIResult();

        _aiServiceMock
            .Setup(x => x.AnalyzeResumeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(aiResult);

        var addedResults = new List<CVScreeningResult>();
        var mockDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<CVScreeningResult>>();
        mockDbSet.Setup(d => d.Add(It.IsAny<CVScreeningResult>()))
            .Callback<CVScreeningResult>(r => addedResults.Add(r));
        _contextMock.Setup(c => c.CVScreeningResults).Returns(mockDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _queue.EnqueueAsync(workItem);

        // Act - Start the service with a cancellation that triggers after processing
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Start the service and wait for it to process one item
        var serviceTask = _service.StartAsync(cts.Token);
        await Task.Delay(1000); // Allow time for processing
        await cts.CancelAsync();

        try { await _service.StopAsync(CancellationToken.None); } catch (OperationCanceledException) { }

        // Assert
        _aiServiceMock.Verify(x => x.AnalyzeResumeAsync(
            workItem.ResumeText, workItem.JobDescription, workItem.RequiredSkills,
            workItem.EducationLevel, workItem.ExperienceLevel), Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        addedResults.Should().ContainSingle();
        addedResults[0].ApplicationId.Should().Be(appId);
        addedResults[0].OverallScore.Should().Be(85);
        addedResults[0].AIModel.Should().Be("qwen-3-32b");
    }

    [Fact]
    public async Task Service_ShouldLogError_WhenAIServiceFails()
    {
        // Arrange
        var workItem = CreateWorkItem();

        _aiServiceMock
            .Setup(x => x.AnalyzeResumeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Groq AI unavailable"));

        await _queue.EnqueueAsync(workItem);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serviceTask = _service.StartAsync(cts.Token);
        await Task.Delay(1000);
        await cts.CancelAsync();

        try { await _service.StopAsync(CancellationToken.None); } catch (OperationCanceledException) { }

        // Assert - should have logged the error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CV scoring failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Should NOT have saved anything to DB
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_ShouldContinueProcessing_AfterOneItemFails()
    {
        // Arrange
        var failItem = CreateWorkItem();
        var successItem = CreateWorkItem();

        _aiServiceMock
            .SetupSequence(x => x.AnalyzeResumeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("First call fails"))
            .ReturnsAsync(CreateAIResult());

        var mockDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<CVScreeningResult>>();
        _contextMock.Setup(c => c.CVScreeningResults).Returns(mockDbSet.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _queue.EnqueueAsync(failItem);
        await _queue.EnqueueAsync(successItem);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var serviceTask = _service.StartAsync(cts.Token);
        await Task.Delay(2000); // Allow time for both items
        await cts.CancelAsync();

        try { await _service.StopAsync(CancellationToken.None); } catch (OperationCanceledException) { }

        // Assert - AI service should have been called twice (once for fail, once for success)
        _aiServiceMock.Verify(x => x.AnalyzeResumeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Exactly(2));

        // DB should have been saved once (only for the successful item)
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
