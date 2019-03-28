using System.Linq;
using FluentValidation;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Validator
{
    public class CustomEffectValidator : AbstractValidator<Entity.CustomEffect>
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
        }
    }
}
