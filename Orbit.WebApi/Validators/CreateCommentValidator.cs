using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class CreateCommentValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(ValidationConstants.ContentRequired)
            .MaximumLength(500).WithMessage(ValidationConstants.ContentMaxLengthComment);
    }
}
