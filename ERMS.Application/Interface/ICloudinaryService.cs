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
}
