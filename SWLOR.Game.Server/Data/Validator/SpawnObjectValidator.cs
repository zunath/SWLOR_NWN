using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class SpawnObjectValidator: AbstractValidator<SpawnObject>
    {
        public SpawnObjectValidator()
        {
            RuleFor(x => x.SpawnID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Resref)
                .NotNull()
                .NotEmpty()
                .MaximumLength(16);

            RuleFor(x => x.Weight)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.SpawnRule)
                .NotNull()
                .MaximumLength(32);

            RuleFor(x => x.BehaviourScript)
                .NotNull()
                .MaximumLength(64);

            RuleFor(x => x.DeathVFXID)
                .NotNull()
                .GreaterThanOrEqualTo(0);
        }
    }
}
