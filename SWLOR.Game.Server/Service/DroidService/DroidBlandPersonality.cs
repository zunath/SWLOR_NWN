using System.Collections.Generic;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidBlandPersonality: IDroidPersonality
    {
        private readonly IRandomService _random;
        private readonly List<string> _greetingPhrases = new()
        {
            "Good to see you again.",
            "Good to see you!",
            "Great to see you!",
            "It's nice to see you!",
            "Look who it is!"
        };

        private readonly List<string> _deathPhrases = new()
        {
            "Dents make me mad.",
            "Ow!",
            "Is this a factory reset?"
        };

        private readonly List<string> _dismissedPhrases = new()
        {
            "Goodbye!",
            "Goodbye, goodbye.",
            "Later, then.",
            "See you again later.",
            "See you around.",
            "See you later.",
            "See you.",
            "So, you have to go?",
            "You're leaving?"
        };

        public DroidBlandPersonality(IRandomService randomService)
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
