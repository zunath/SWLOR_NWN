using System.Linq;
using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class SkillValidator : AbstractValidator<Skill>
    {
        public SkillValidator()
        {
            int[] validAttributes = { 0, 1, 2, 3, 4, 5, 6 };

            RuleFor(x => x.SkillCategoryID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);

            RuleFor(x => x.MaxRank)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Description)
                .NotNull()
                .NotEmpty()
                .MaximumLength(1024);

            RuleFor(x => x.Primary)
                .NotNull()
                .NotEmpty()
                .Must(x => validAttributes.Contains(x))
                .WithMessage("Primary attribute value is invalid.");
            
            RuleFor(x => x.Secondary)
                .NotNull()
                .NotEmpty()
                .Must(x => validAttributes.Contains(x))
                .WithMessage("Secondary attribute value is invalid.");

            RuleFor(x => x.Tertiary)
                .NotNull()
                .NotEmpty()
                .Must(x => validAttributes.Contains(x))
                .WithMessage("Tertiary attribute value is invalid.");


        }
    }
}
