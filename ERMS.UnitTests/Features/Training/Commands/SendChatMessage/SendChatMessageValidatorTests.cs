using ERMS.Application.Features.Training.Commands.SendChatMessage;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace ERMS.UnitTests.Features.Training.Commands.SendChatMessage;

public class SendChatMessageValidatorTests
{
    [Fact]
    public async Task Validate_ShouldRejectEmptyMessage()
    {
        var validator = new SendChatMessageValidator();

        var result = await validator.ValidateAsync(new SendChatMessageCommand
        {
            Message = ""
        });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ShouldRejectOverlongMessage()
    {
        var validator = new SendChatMessageValidator();

        var result = await validator.ValidateAsync(new SendChatMessageCommand
        {
            Message = new string('a', 2001)
        });

        result.IsValid.Should().BeFalse();
    }
}
