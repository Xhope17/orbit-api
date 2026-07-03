using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;
using Orbit.Shared.Constants;

namespace Orbit.WebApi.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(ValidationConstants.ContentRequired)
            .MaximumLength(DomainConstants.MessageContentMaxLength).WithMessage(ValidationConstants.ContentMaxLengthMessage);
    }
}
