using FluentValidation;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Data.Validator
{
    public class CraftBlueprintValidator : AbstractValidator<CraftBlueprint>
    {
        public CraftBlueprintValidator()
        {
            RuleFor(x => x.CraftCategoryID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.BaseLevel)
                .NotNull();

            RuleFor(x => x.ItemName)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.ItemResref)
                .NotNull()
                .NotEmpty()
                .MaximumLength(16);

            RuleFor(x => x.Quantity)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.SkillID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.CraftDeviceID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PerkID)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MainComponentTypeID)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MainMinimum)
                .NotNull()
                .GreaterThanOrEqualTo(0);


            RuleFor(x => x.SecondaryComponentTypeID)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.SecondaryMinimum)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.TertiaryComponentTypeID)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.TertiaryMinimum)
                .NotNull()
                .GreaterThanOrEqualTo(0);


            RuleFor(x => x.EnhancementSlots)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MainMaximum)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.SecondaryMaximum)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.TertiaryMaximum)
                .NotNull()
                .GreaterThanOrEqualTo(0);




        }
    }
}
