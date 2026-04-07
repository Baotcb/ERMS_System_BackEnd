using System.Threading.Channels;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// In-memory background task queue backed by a Channel for CV scoring work items.
/// Registered as a Singleton.
/// </summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<CvScoringWorkItem> _channel =
        Channel.CreateUnbounded<CvScoringWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public ValueTask EnqueueAsync(CvScoringWorkItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public ValueTask<CvScoringWorkItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}

/// <summary>
/// Background service that dequeues CV scoring work items and processes them
/// using Gemini AI. Runs for the entire application lifetime.
/// </summary>
public sealed class CvScoringBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CvScoringBackgroundService> _logger;

    public CvScoringBackgroundService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<CvScoringBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dịch vụ nền tính điểm CV đang khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            CvScoringWorkItem? workItem = null;

            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Đang xử lý tính điểm CV cho hồ sơ {ApplicationId}.",
                    workItem.ApplicationId);

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IERMSDbContext>();
                var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiAIService>();

                var aiResult = await geminiService.AnalyzeResumeAsync(
                    workItem.ResumeText,
                    workItem.JobDescription,
                    workItem.RequiredSkills,
                    workItem.EducationLevel,
                    workItem.ExperienceLevel);

                var screeningResult = new CVScreeningResult
                {
                    Id = Guid.CreateVersion7(),
                    ApplicationId = workItem.ApplicationId,
                    OverallScore = aiResult.OverallScore,
                    SkillMatchScore = aiResult.SkillMatchScore,
                    ExperienceMatchScore = aiResult.ExperienceMatchScore,
                    EducationMatchScore = aiResult.EducationMatchScore,
                    KeywordMatchScore = aiResult.KeywordMatchScore,
                    MatchedSkills = JsonSerializer.Serialize(aiResult.MatchedSkills),
                    MissingSkills = JsonSerializer.Serialize(aiResult.MissingSkills),
                    Strengths = JsonSerializer.Serialize(aiResult.Strengths),
                    Concerns = JsonSerializer.Serialize(aiResult.Concerns),
                    Summary = aiResult.Summary,
                    RawResponse = aiResult.RawResponse,
                    ProcessedAt = DateTime.UtcNow,
                    AIModel = "gemini-2.5-flash"
                };

                dbContext.CVScreeningResults.Add(screeningResult);
                await dbContext.SaveChangesAsync(CancellationToken.None);

                _logger.LogInformation(
                    "Tính điểm CV hoàn tất cho hồ sơ {ApplicationId}. Điểm tổng quan: {Score}",
                    workItem.ApplicationId, aiResult.OverallScore);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — exit the loop
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Tính điểm CV thất bại cho hồ sơ {ApplicationId}. Hồ sơ đã được lưu — HR có thể kích hoạt tính điểm lại thủ công.",
                    workItem?.ApplicationId);

                // Continue processing the next item in the queue
            }
        }

        _logger.LogInformation("Dịch vụ nền tính điểm CV đang dừng lại.");
    }
}
