using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Associate.Model
{
    public class DroidPrissyPersonality: IDroidPersonality
    {
        private readonly IServiceProvider _serviceProvider;

        public DroidPrissyPersonality(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
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
            return _deathPhrases[Random.Next(_deathPhrases.Count)];
        }

        public string DismissedPhrase()
        {
            return _dismissedPhrases[Random.Next(_dismissedPhrases.Count)];
        }
    }
}
