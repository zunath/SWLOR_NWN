using System.Linq;
using FluentValidation;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Validator
{
    public class CustomEffectValidator : AbstractValidator<CustomEffect>
    {
        public CustomEffectValidator()
        {
            CustomEffectCategoryType[] validCategories =
            {
                CustomEffectCategoryType.NormalEffect,
                CustomEffectCategoryType.FoodEffect,
                CustomEffectCategoryType.Stance
            };

            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);

            RuleFor(x => x.IconID)
                .Equal(0)
                .NotNull();

            RuleFor(x => x.ScriptHandler)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.StartMessage)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.ContinueMessage)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.WornOffMessage)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.CustomEffectCategoryID)
                .Must(x => validCategories.Contains((CustomEffectCategoryType) x))
                .WithMessage("CustomEffectCategoryID not found in list of valid values.");
        }
    }
}
