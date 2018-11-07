using FluentValidation;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Data.Validator
{
    public class CooldownCategoryValidator : AbstractValidator<CooldownCategory>
    {
        public CooldownCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.BaseCooldownTime)
                .GreaterThan(0);

        }
    }
}
