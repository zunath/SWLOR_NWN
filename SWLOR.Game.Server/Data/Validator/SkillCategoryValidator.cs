using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class SkillCategoryValidator : AbstractValidator<SkillCategory>
    {
        public SkillCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);

            RuleFor(x => x.Sequence)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);
        }
    }
}
