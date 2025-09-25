using SWLOR.Component.Associate.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Component.Associate.Model
{
    public class DroidGeekyPersonality: IDroidPersonality
    {
        private readonly IRandomService _random;
        private readonly List<string> _greetingPhrases = new()
        {
            "Approaching entity: this unit's databank shows you as welcome.",
            "Engaging communication protocol: PROT_LIKE.",
            "Entity being tracked by sensors.",
            "Executing greet procedures (cheerful modifiers). Selecting random message. Random message not found.",
            "Friendly entity being tracked by sensors.",
            "Greeting subroutines now active.",
            "If isOnLikeList((target == breachOfSensors())) executeProcedure(\"greet,\" target).",
            "PROT_LIKE standard response: \"Hey, it is great to see you.\""
        };

        private readonly List<string> _deathPhrases = new()
        {
            "FATAL! Overheating core.",
            "Exterior chassis sensors note a new dent.",
            "DEBUG:: external shock to system causing memory core loss."
        };

        private readonly List<string> _dismissedPhrases = new()
        {
            "Goodbye, entity.",
            "Goodbye Sentient.",
            "Optic sensors detect withdrawal of entity."
        };

        public DroidGeekyPersonality(IRandomService randomService)
        {
            _random = randomService;
        }

        public string GreetingPhrase()
        {
            return _greetingPhrases[_random.Next(_greetingPhrases.Count)];
        }

        public string DeathPhrase()
        {
            return _deathPhrases[_random.Next(_deathPhrases.Count)];
        }

        public string DismissedPhrase()
        {
            return _dismissedPhrases[_random.Next(_dismissedPhrases.Count)];
        }
    }
}
