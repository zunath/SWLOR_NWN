using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class CraftDeviceValidator : AbstractValidator<CraftDevice>
    {
        public CraftDeviceValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);
        }
    }
}
