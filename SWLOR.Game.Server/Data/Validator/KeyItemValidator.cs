using FluentValidation;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Data.Validator
{
    public class KeyItemValidator : AbstractValidator<KeyItem>
    {
        public KeyItemValidator()
        {
            RuleFor(x => x.KeyItemCategoryID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.Description)
                .NotNull()
                .NotEmpty()
                .MaximumLength(1000);
        }
    }
}
