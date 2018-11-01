using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class ModValidator : AbstractValidator<Mod>
    {
        public ModValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.Script)
                .NotNull()
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}
