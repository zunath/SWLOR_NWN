using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class BuildingStyleValidator : AbstractValidator<BuildingStyle>
    {
        public BuildingStyleValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.Resref)
                .NotNull()
                .NotEmpty()
                .MaximumLength(16);

            RuleFor(x => x.DoorRule)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.PurchasePrice)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.DailyUpkeep)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.FurnitureLimit)
                .GreaterThanOrEqualTo(1);
        }
    }
}
