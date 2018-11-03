using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class ApartmentBuildingValidator: AbstractValidator<ApartmentBuilding>
    {
        public ApartmentBuildingValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(64);
        }
    }
}
