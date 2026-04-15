using System.Text;
using ERMS.Application.Features.Applications.Commands.ExtractCVInfo;
using ERMS.Application.Interface;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace ERMS.UnitTests.Features.Applications.Commands.ExtractCVInfo;

public class ExtractCVInfoHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnResumePublicId_AndParsedCandidateInfo()
    {
        // Arrange
        var cloudinaryServiceMock = new Mock<ICloudinaryService>();
        var pdfTextExtractorMock = new Mock<IPdfTextExtractor>();
        var cvParsingServiceMock = new Mock<ICVParsingService>();
        var loggerMock = new Mock<ILogger<ExtractCVInfoHandler>>();

        cloudinaryServiceMock
            .Setup(x => x.UploadPdfAsync(It.IsAny<Stream>(), "candidate.pdf"))
            .ReturnsAsync(("https://res.cloudinary.com/erms/candidate.pdf", "erms/resumes/candidate"));

        pdfTextExtractorMock
            .Setup(x => x.ExtractTextAsync(It.IsAny<Stream>()))
            .ReturnsAsync("John Doe .NET Engineer");

        cvParsingServiceMock
            .Setup(x => x.ParseCVAsync("John Doe .NET Engineer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CVParsedInfoDto
            {
                FullName = "John Doe",
                Email = "john.doe@gmail.com",
                PhoneNumber = "0901234567"
            });

        var handler = new ExtractCVInfoHandler(
            cloudinaryServiceMock.Object,
            pdfTextExtractorMock.Object,
            cvParsingServiceMock.Object,
            loggerMock.Object);

        var command = new ExtractCVInfoCommand
        {
            CvFile = CreateFormFile("candidate.pdf")
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ResumeUrl.Should().Be("https://res.cloudinary.com/erms/candidate.pdf");
        result.ResumePublicId.Should().Be("erms/resumes/candidate");
        result.ResumeText.Should().Be("John Doe .NET Engineer");
        result.FullName.Should().Be("John Doe");
        result.Email.Should().Be("john.doe@gmail.com");
        result.Phone.Should().Be("0901234567");

        cloudinaryServiceMock.Verify(x => x.UploadPdfAsync(It.IsAny<Stream>(), "candidate.pdf"), Times.Once);
        pdfTextExtractorMock.Verify(x => x.ExtractTextAsync(It.IsAny<Stream>()), Times.Once);
        cvParsingServiceMock.Verify(x => x.ParseCVAsync("John Doe .NET Engineer", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IFormFile CreateFormFile(string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes("fake-pdf-content");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "cvFile", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }
}
