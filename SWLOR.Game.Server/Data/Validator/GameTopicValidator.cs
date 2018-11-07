using FluentValidation;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Data.Validator
{
    public class GameTopicValidator: AbstractValidator<GameTopic>
    {
        public GameTopicValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(32);

            RuleFor(x => x.Text)
                .NotNull()
                .NotEmpty();

            RuleFor(x => x.GameTopicCategoryID)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Sequence)
                .NotNull()
                .NotEmpty()
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Icon)
                .NotNull()
                .MaximumLength(32);
        }
    }
}
