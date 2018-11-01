using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class LootTableValidator : AbstractValidator<LootTable>
    {
        public LootTableValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleForEach(x => x.LootTableItems)
                .SetValidator(new LootTableItemValidator());
        }
    }
}
