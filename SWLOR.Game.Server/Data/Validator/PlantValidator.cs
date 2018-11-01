using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class PlantValidator : AbstractValidator<Plant>
    {
        public PlantValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);
            RuleFor(x => x.BaseTicks)
                .NotNull()
                .NotEmpty();
            RuleFor(x => x.Resref)
                .NotNull()
                .NotEmpty()
                .MaximumLength(16);
            RuleFor(x => x.WaterTicks)
                .NotNull()
                .NotEmpty();
            RuleFor(x => x.Level)
                .NotNull()
                .LessThanOrEqualTo(50);
            RuleFor(x => x.SeedResref)
                .NotNull()
                .NotEmpty()
                .MaximumLength(16);
        }
    }
}
