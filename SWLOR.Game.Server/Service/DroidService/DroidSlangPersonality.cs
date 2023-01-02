using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class DroidSlangPersonality: IDroidPersonality
    {
        private readonly List<string> _greetingPhrases = new()
        {
            "Good t'see ya!",
            "Hey there, buddy!",
            "Hullo! Right nice to see ya!",
            "Hullo! Yer needin' help?",
            "'S great to see yer!",
            "'S wunnerful to see yer again!",
            "Well, hullo!",
            "Well, yer a sight for my sore eyes!"
        };

        private readonly List<string> _deathPhrases = new()
        {
            "Hey, is there a mechanic in the house?",
            "Hurm, this could be gettin' bad by and by.",
            "Jest a scratch.",
            "Not good ... not good.",
            "Ouch.",
            "Why you little..."
        };

        private readonly List<string> _dismissedPhrases = new()
        {
            "Bye now.",
            "Bye-bye.",
            "G'bye.",
            "Got a date? See you later.",
            "See ya later.",
            "Well now. See ya later.",
            "Wuz an honor ter see ya. Honest."
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
