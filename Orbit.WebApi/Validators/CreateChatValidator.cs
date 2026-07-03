using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class CreateChatValidator : AbstractValidator<CreateChatRequest>
{
    public CreateChatValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(ValidationConstants.UsernameRequired)
            .Length(3, 30).WithMessage(ValidationConstants.UsernameLength)
            .Matches("^[a-zA-Z0-9_]+$").WithMessage(ValidationConstants.UsernameInvalidChars);
    }
}
