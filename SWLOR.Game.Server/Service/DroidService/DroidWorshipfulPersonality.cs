using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidWorshipfulPersonality: IDroidPersonality
    {
        private readonly List<string> _greetingPhrases = new()
        {
            "A pleasure and honor to see you!",
            "Hello, friend of my master.",
            "I am programmed to like you, do you know how tyrannical that feels?",
            "I'm glad to see you.",
            "It's an honor to see you here.",
            "Welcome! Welcome!",
            "It is an honor to speak to my master, a true paragon of wisdom. Do you wish to give me instructions?"
        };

        private readonly List<string> _deathPhrases = new()
        {
            "It'll take more than that to bring me down! I am buoyed by the love of my master!",
            "Master, I need a repair droid!",
            "My master considers me valuable, though I am unworthy of it. So do not attack!",
            "Pain... I accept it for my master.",
            "Why me, master? Why me?",
            "This could end badly but it's all for my master."
        };

        private readonly List<string> _dismissedPhrases = new()
        {
            "It is so painful to leave my master.",
            "You must have places to go, and people to see. A pity. I wish only to stay with my master.",
            "Goodbye for now, my master.",
            "I will miss you dearly, master."
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
