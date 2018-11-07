using FluentValidation;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Data.Validator
{
    public class LootTableItemValidator : AbstractValidator<LootTableItem>
    {
        public LootTableItemValidator()
        {
            RuleFor(x => x.Resref)
                .NotNull()
                .NotEmpty()
                .MaximumLength(16);

            RuleFor(x => x.MaxQuantity)
                .NotNull()
                .NotEmpty()
                .LessThanOrEqualTo(99);

            RuleFor(x => x.Weight)
                .NotNull()
                .NotEmpty()
                .LessThanOrEqualTo((byte) 255)
                .GreaterThanOrEqualTo((byte)1);

            RuleFor(x => x.SpawnRule)
                .NotNull()
                .MaximumLength(64);
        }
    }
}
