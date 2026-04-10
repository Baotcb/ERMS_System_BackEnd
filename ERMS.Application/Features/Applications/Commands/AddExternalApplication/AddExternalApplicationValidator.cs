using FluentValidation;

namespace ERMS.Application.Features.Applications.Commands.AddExternalApplication;

public sealed class AddExternalApplicationValidator : AbstractValidator<AddExternalApplicationCommand>
{
    public AddExternalApplicationValidator()
    {
        RuleFor(x => x.JobPostingId)
            .NotEmpty().WithMessage("Vui lòng chọn tin tuyển dụng.");

        RuleFor(x => x.CandidateName)
            .NotEmpty().WithMessage("Tên ứng viên không được để trống.")
            .MaximumLength(200).WithMessage("Tên ứng viên không được vượt quá 200 ký tự.");

        RuleFor(x => x.CandidateEmail)
            .NotEmpty().WithMessage("Email ứng viên không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(200).WithMessage("Email không được vượt quá 200 ký tự.");

        RuleFor(x => x.CandidatePhone)
            .MaximumLength(20).WithMessage("Số điện thoại không được vượt quá 20 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.CandidatePhone));

        RuleFor(x => x.ResumeUrl)
            .NotEmpty().WithMessage("URL CV không được để trống.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("URL CV phải là đường dẫn HTTP/HTTPS hợp lệ.");

        RuleFor(x => x.ResumeText)
            .NotEmpty().WithMessage("Nội dung CV không được để trống.");
    }
}
