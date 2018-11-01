using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class GameTopicCategoryValidator : AbstractValidator<GameTopicCategory>
    {
        public GameTopicCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);
        }
    }
}
