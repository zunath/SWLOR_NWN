using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidPrissyPersonality: IDroidPersonality
    {
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



        public string GreetingPhrase()
        {
            return _greetingPhrases[Random.Next(_greetingPhrases.Count)];
        }

        public string DeathPhrase()
        {
            return _greetingPhrases[Random.Next(_deathPhrases.Count)];
        }

        public string DismissedPhrase()
        {
            return _greetingPhrases[Random.Next(_dismissedPhrases.Count)];
        }
    }
}
