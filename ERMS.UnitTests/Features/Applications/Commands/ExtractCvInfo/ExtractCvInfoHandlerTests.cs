using System.Text;
using ERMS.Application.Features.Applications.Commands.ExtractCvInfo;
using ERMS.Application.Interface;
using ERMS.Domain.Constants.Roles;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace ERMS.UnitTests.Features.Applications.Commands.ExtractCvInfo;

public class ExtractCvInfoHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPassPdfBytesToCvInfoExtractor_WhenPdfTextIsEmpty()
    {
        // Arrange
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.7 sample");
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(x => x.OpenReadStream()).Returns(() => new MemoryStream(pdfBytes));
        fileMock.Setup(x => x.FileName).Returns("resume.pdf");

        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(x => x.Roles).Returns([AppRoles.HRManager]);

        var cloudinaryService = new Mock<ICloudinaryService>();
        cloudinaryService
            .Setup(x => x.UploadPdfAsync(It.IsAny<Stream>(), "resume.pdf"))
            .ReturnsAsync(("https://cloudinary.example/resume.pdf", "resume-public-id"));

        var pdfTextExtractor = new Mock<IPdfTextExtractor>();
        pdfTextExtractor.Setup(x => x.ExtractTextAsync(It.IsAny<Stream>())).ReturnsAsync(string.Empty);

        var cvInfoExtractor = new Mock<ICvInfoExtractorService>();
        cvInfoExtractor
            .Setup(x => x.ExtractContactInfoAsync(
                string.Empty,
                It.Is<byte[]>(bytes => bytes.SequenceEqual(pdfBytes)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedCvInfoDto
            {
                FullName = "Hieu Lul",
                Email = "fafac191@gmail.com",
                Phone = "0386708860"
            });

        var handler = new ExtractCvInfoHandler(
            currentUserService.Object,
            cloudinaryService.Object,
            pdfTextExtractor.Object,
            cvInfoExtractor.Object,
            Mock.Of<ILogger<ExtractCvInfoHandler>>());

        var command = new ExtractCvInfoCommand
        {
            CvFile = fileMock.Object
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ResumeUrl.Should().Be("https://cloudinary.example/resume.pdf");
        result.ResumePublicId.Should().Be("resume-public-id");
        result.FullName.Should().Be("Hieu Lul");
        result.Email.Should().Be("fafac191@gmail.com");
        result.Phone.Should().Be("0386708860");
    }
}
