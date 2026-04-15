using System.Text.Json;
using System.Threading.Channels;
using ERMS.Application.Interface;
using ERMS.Domain.Entities.Application;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
/// Background service that dequeues CV scoring work items and processes them.
/// </summary>
public sealed class CvScoringBackgroundService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CvScoringBackgroundService> _logger;
    private readonly GroqModelSettings _groqModelSettings;

    public CvScoringBackgroundService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<CvScoringBackgroundService> logger,
        IOptions<GroqModelSettings> groqModelOptions)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _groqModelSettings = groqModelOptions.Value;
    }

    public CvScoringBackgroundService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<CvScoringBackgroundService> logger)
        : this(
            queue,
            scopeFactory,
            logger,
            Microsoft.Extensions.Options.Options.Create(new GroqModelSettings()))
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background service chấm điểm CV đang khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            CvScoringWorkItem? workItem = null;

            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation(
                    "Đang xử lý chấm điểm CV cho đơn ứng tuyển {ApplicationId}.",
                    workItem.ApplicationId);

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IERMSDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();

                var aiResult = await aiService.AnalyzeResumeAsync(
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
                    AIModel = ResolveScoringModel()
                };

                dbContext.CVScreeningResults.Add(screeningResult);
                await dbContext.SaveChangesAsync(CancellationToken.None);

                _logger.LogInformation(
                    "Hoàn tất chấm điểm CV cho đơn ứng tuyển {ApplicationId}. Điểm tổng: {Score}",
                    workItem.ApplicationId, aiResult.OverallScore);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi chấm điểm CV cho đơn ứng tuyển {ApplicationId}.",
                    workItem?.ApplicationId);
            }
        }

        _logger.LogInformation("Background service chấm điểm CV đang dừng.");
    }

    private string ResolveScoringModel()
    {
        if (!string.IsNullOrWhiteSpace(_groqModelSettings.CvScoring))
        {
            return _groqModelSettings.CvScoring.Trim();
        }

        throw new InvalidOperationException("Thiếu cấu hình model cho CV scoring (GroqModels:CvScoring).");
    }
}
