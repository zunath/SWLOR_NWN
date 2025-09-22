using SWLOR.Component.Associate.Contracts;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Component.Associate.Model
{
    public class DroidPrissyPersonality: IDroidPersonality
    {
        private readonly IRandomService _random;
        private readonly List<string> _greetingPhrases = new()
        {
            "Greetings! May I be of assistance?",
            "I'm honored to see you here.",
            "It is a pleasure and honor to greet you.",
            "It is good to see you.",
            "It is, as they say, quite nice to see you.",
            "Welcome it is a pleasure to see you here."
        };

        private readonly List<string> _deathPhrases = new()
        {
            "My pain receptors are overloading...",
            "Oh dear. That might need to be buffed out."
        };

        private readonly List<string> _dismissedPhrases = new()
        {
            "Farewell.",
            "Ta ta for now!",
            "Ta ta, then."
        };

        public DroidPrissyPersonality(IRandomService randomService)
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
