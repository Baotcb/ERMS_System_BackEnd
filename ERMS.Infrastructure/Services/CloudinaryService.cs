using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ERMS.Application.Interface;
using ERMS.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// Cloudinary service for file uploads
/// API credentials loaded from environment variables for security
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly CloudinarySettings _settings;

    public CloudinaryService(ILogger<CloudinaryService> logger, IOptions<CloudinarySettings> options)
    {
        _logger = logger;
        _settings = options.Value;

        if (string.IsNullOrWhiteSpace(_settings.CloudName))
            throw new InvalidOperationException("Chưa cấu hình Cloudinary CloudName trong appsettings.");
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Chưa cấu hình Cloudinary ApiKey trong appsettings.");
        if (string.IsNullOrWhiteSpace(_settings.ApiSecret))
            throw new InvalidOperationException("Chưa cấu hình Cloudinary ApiSecret trong appsettings.");

        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(string Url, string PublicId)> UploadPdfAsync(Stream fileStream, string fileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sanitizedFileName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("-", "_");

        var publicId = $"erms/resumes/{timestamp}_{sanitizedFileName}";

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = publicId,
            Overwrite = false
        };

        _logger.LogInformation("Đang tải PDF lên Cloudinary: {PublicId}", publicId);

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            _logger.LogError("Tải lên Cloudinary thất bại: {Error}", uploadResult.Error.Message);
            throw new Exception($"Tải tệp lên Cloudinary thất bại: {uploadResult.Error.Message}");
        }

        _logger.LogInformation("Đã tải PDF lên thành công. URL: {Url}", uploadResult.SecureUrl);

        return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    public async Task<(string Url, string PublicId, int DurationMinutes)> UploadVideoAsync(Stream fileStream, string fileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var sanitizedFileName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("-", "_");

        var publicId = $"erms/lessons/{timestamp}_{sanitizedFileName}";

        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            PublicId = publicId,
            Overwrite = false,

            // tối ưu video
            Transformation = new Transformation()
                .Quality("auto")
                .FetchFormat("auto")
        };

        _logger.LogInformation("Đang tải video lên Cloudinary: {PublicId}", publicId);

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
        {
            _logger.LogError("Tải video lên thất bại: {Error}", result.Error.Message);
            throw new Exception(result.Error.Message);
        }

        var durationMinutes = (int)Math.Ceiling(result.Duration / 60);

        return (
            result.SecureUrl.ToString(),
            result.PublicId,
            durationMinutes
        );
    }
}
