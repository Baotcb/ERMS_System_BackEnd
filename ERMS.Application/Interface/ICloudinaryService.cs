namespace ERMS.Application.Interface;

/// <summary>
/// Interface for Cloudinary file upload operations
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Uploads a PDF file to Cloudinary
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Tuple of (FileUrl, PublicId)</returns>
    Task<(string Url, string PublicId)> UploadPdfAsync(Stream fileStream, string fileName);
    Task<(string Url, string PublicId, int DurationMinutes)> UploadVideoAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Creates a time-limited signed URL for downloading a stored asset.
    /// </summary>
    /// <param name="fileUrl">Stored Cloudinary delivery URL.</param>
    /// <param name="fileName">Original file name used to infer the format if needed.</param>
    /// <param name="expiresIn">Optional TTL for the signed URL.</param>
    /// <returns>Signed Cloudinary download URL.</returns>
    string GetAuthenticatedDownloadUrl(string fileUrl, string fileName, TimeSpan? expiresIn = null);
}
