using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidSarcasticPersonality: IDroidPersonality
    {
        private readonly List<string> _greetingPhrases = new()
        {
            "Can I help you?",
            "I'm sure it's great to see you. Sorry if I sound depressed. I'm always depressed.",
            "It's a pleasure to see you here. You'll ignore me, of course. But I can't help it, I'm programmed to like you.",
            "It's good to see you. You don't care if you see ME, of course. I'm just a lonely droid...",
            "It's wonderful to see you. Maybe you'll just loosen this restraining bolt here ... no? Oh well.",
            "Welcome.",
            "Well, it's nice to meet you before I rust away."
        };

        private readonly List<string> _deathPhrases = new()
        {
            "A touch, I do confess it. A veritable ouchie.",
            "I could use a repair droid around now.",
            "If I close my eyes maybe this will all go away...",
            "Look at that, scratches on my finish.",
            "Oh boy, that hurts. Not that anyone cares if a droid feels pain.",
            "Ouch.",
            "Why me?"
        };

        private readonly List<string> _dismissedPhrases = new()
        {
            "Got somewhere to be? Sure, go spend time away from the droid.",
            "I'm sure I'll see you again. Knowing how droids get treated though, you may just avoid me.",
            "Later, then.",
            "Oh, you're leaving? Nobody ever wants to stay with droids."
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
