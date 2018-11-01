using FluentValidation;

namespace SWLOR.Game.Server.Data.Validator
{
    public class DownloadValidator : AbstractValidator<Download>
    {
        public DownloadValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Description)
                .NotNull()
                .NotEmpty()
                .MaximumLength(1000);

            RuleFor(x => x.Url)
                .NotNull()
                .NotEmpty()
                .MaximumLength(200);

        }
    }
}
