using System.Linq;
using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class SpawnValidator : AbstractValidator<Spawn>
    {
        public SpawnValidator()
        {
            int[] validObjectTypes = {-1, 1, 64};

            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.SpawnObjectTypeID)
                .NotNull()
                .NotEmpty()
                .Must(x => validObjectTypes.Contains(x))
                .WithMessage("Spawn object type is invalid.");

            RuleForEach(x => x.SpawnObjects)
                .SetValidator(new SpawnObjectValidator());
        }
    }
}
