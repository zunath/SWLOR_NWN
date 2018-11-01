using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class FameRegionValidator : AbstractValidator<FameRegion>
    {
        public FameRegionValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);
        }
    }
}
