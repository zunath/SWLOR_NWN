using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class KeyItemCategoryValidator : AbstractValidator<KeyItemCategory>
    {
        public KeyItemCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);
        }
    }
}
