using FluentValidation;
using SWLOR.Game.Server.Data.Entity;

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
