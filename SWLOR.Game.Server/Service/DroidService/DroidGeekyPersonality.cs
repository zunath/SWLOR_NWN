using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidGeekyPersonality: IDroidPersonality
    {
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



        public string GreetingPhrase()
        {
            return _greetingPhrases[Random.Next(_greetingPhrases.Count)];
        }

        public string DeathPhrase()
        {
            return _deathPhrases[Random.Next(_deathPhrases.Count)];
        }

        public string DismissedPhrase()
        {
            return _dismissedPhrases[Random.Next(_dismissedPhrases.Count)];
        }
    }
}
