using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidBlandPersonality: IDroidPersonality
    {
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
