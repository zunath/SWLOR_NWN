using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class CraftBlueprintCategoryValidator : AbstractValidator<CraftBlueprintCategory>
    {
        public CraftBlueprintCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);
        }
    }
}
