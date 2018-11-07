using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class ModValidator : AbstractValidator<Entity.Mod>
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
