using FluentValidation;
using Orbit.WebApi.Constants;
using Orbit.WebApi.Models;

namespace Orbit.WebApi.Validators;

public class CreateCommunityValidator : AbstractValidator<CreateCommunityRequest>
{
    public CreateCommunityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ValidationConstants.CommunityNameRequired)
            .MaximumLength(100).WithMessage(ValidationConstants.CommunityNameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null)
            .WithMessage(ValidationConstants.CommunityDescriptionMaxLength);
    }
}

public class UpdateCommunityValidator : AbstractValidator<UpdateCommunityRequest>
{
    public UpdateCommunityValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationConstants.CommunityNameRequired)
                .MaximumLength(100).WithMessage(ValidationConstants.CommunityNameMaxLength);
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage(ValidationConstants.CommunityDescriptionMaxLength);
        });
    }
}
