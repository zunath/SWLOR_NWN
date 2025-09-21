using System.Collections.Generic;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidPrissyPersonality: IDroidPersonality
    {
        private static readonly IRandomService _random = ServiceContainer.GetService<IRandomService>();
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
