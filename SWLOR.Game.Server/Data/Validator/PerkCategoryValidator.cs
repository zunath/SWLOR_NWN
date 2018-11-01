using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class PerkCategoryValidator : AbstractValidator<PerkCategory>
    {
        public PerkCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.Sequence)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);
        }
    }
}
