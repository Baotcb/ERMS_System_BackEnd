using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ERMS.Application.Interface;
using Microsoft.Extensions.Logging;

namespace ERMS.Infrastructure.Services;

/// <summary>
/// Cloudinary service for file uploads
/// API credentials loaded from environment variables for security
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        // Load credentials from environment variables (NEVER from appsettings)
        var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? throw new InvalidOperationException("CLOUDINARY_CLOUD_NAME environment variable is not set.");
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? throw new InvalidOperationException("CLOUDINARY_API_KEY environment variable is not set.");
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? throw new InvalidOperationException("CLOUDINARY_API_SECRET environment variable is not set.");

        var account = new Account(cloudName, apiKey, apiSecret);
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
