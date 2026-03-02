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
            throw new InvalidOperationException("Cloudinary CloudName is not configured in appsettings.");
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Cloudinary ApiKey is not configured in appsettings.");
        if (string.IsNullOrWhiteSpace(_settings.ApiSecret))
            throw new InvalidOperationException("Cloudinary ApiSecret is not configured in appsettings.");

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

        _logger.LogInformation("Uploading PDF to Cloudinary: {PublicId}", publicId);

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
            throw new Exception($"Failed to upload file to Cloudinary: {uploadResult.Error.Message}");
        }

        _logger.LogInformation("PDF uploaded successfully. URL: {Url}", uploadResult.SecureUrl);

        return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }
}
