using FluentValidation;

namespace ERMS.Application.Features.JobPostings.Commands.UpdateJobPosting;

public sealed class UpdateJobPostingValidator : AbstractValidator<UpdateJobPostingCommand>
{
    // "FullTime" được giữ lại để tương thích với dữ liệu cũ (entity default là "FullTime")
    private static readonly string[] ValidEmploymentTypes = ["Full-time", "Part-time", "Contract", "Internship", "FullTime"];

    public UpdateJobPostingValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID tin tuyển dụng là bắt buộc.");

        RuleFor(x => x.JobTitle)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.JobTitle));

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Requirements)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Requirements));

        RuleFor(x => x.Benefits)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Benefits));

        RuleFor(x => x.EmploymentType)
            .Must(t => ValidEmploymentTypes.Contains(t!))
            .When(x => !string.IsNullOrEmpty(x.EmploymentType))
            .WithMessage($"Hình thức làm việc không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", ValidEmploymentTypes)}.");

        RuleFor(x => x.Location)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.ApplicationDeadline)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ApplicationDeadline.HasValue)
            .WithMessage("Hạn nộp hồ sơ phải trong tương lai.");

        RuleFor(x => x.SalaryRangeMin)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SalaryRangeMin.HasValue)
            .WithMessage("Mức lương tối thiểu phải >= 0.");

        RuleFor(x => x.SalaryRangeMax)
            .Must((cmd, max) => max == null || cmd.SalaryRangeMin == null || max >= cmd.SalaryRangeMin)
            .When(x => x.SalaryRangeMax.HasValue)
            .WithMessage("Mức lương tối đa phải >= mức lương tối thiểu.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Quantity.HasValue)
            .WithMessage("Số lượng tuyển phải > 0.");
    }
}
