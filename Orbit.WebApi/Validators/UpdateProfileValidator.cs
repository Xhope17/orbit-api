using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .MaximumLength(100).WithMessage(ValidationConstants.DisplayNameMaxLength);
        });

        When(x => x.Bio is not null, () =>
        {
            RuleFor(x => x.Bio!)
                .MaximumLength(500).WithMessage(ValidationConstants.BioMaxLength);
        });
    }
}
