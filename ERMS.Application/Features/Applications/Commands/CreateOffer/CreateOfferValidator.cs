using FluentValidation;
using System;

namespace ERMS.Application.Features.Applications.Commands.CreateOffer
{
    public class CreateOfferValidator : AbstractValidator<CreateOfferCommand>
    {
        public CreateOfferValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId là bắt buộc.");

            RuleFor(x => x.Position)
                .NotEmpty().WithMessage("Vị trí là bắt buộc.")
                .MaximumLength(200).WithMessage("Vị trí không được vượt quá 200 ký tự.");

            RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Lương phải lớn hơn 0.");

            RuleFor(x => x.SalaryFrequency)
                .NotEmpty().WithMessage("Tần suất trả lương là bắt buộc.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Ngày bắt đầu là bắt buộc.");

            RuleFor(x => x.ExpirationDate)
                .NotEmpty().WithMessage("Ngày hết hạn là bắt buộc.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Ngày hết hạn phải sau thời điểm hiện tại.");
        }
    }
}