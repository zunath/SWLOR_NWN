using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>A situation definition: its text, when it can be drawn, its choices, and what happens if nobody decides.</summary>
    public sealed class SituationDetail
    {
        public SituationDetail(SituationType type)
        {
            Type = type;
            Title = string.Empty;
            Text = string.Empty;
            IgnoredGazetteText = string.Empty;
            Weight = SituationRules.DefaultWeight;
            Requirements = new List<SituationRequirement>();
            Choices = new List<SituationChoice>();
            IgnoredEffects = new List<SituationEffect>();
        }

        public SituationType Type { get; }

        public string Title { get; set; }

        /// <summary>What the officials are told.</summary>
        public string Text { get; set; }

        /// <summary>How likely the situation is among those the city is eligible for.</summary>
        public int Weight { get; set; }

        /// <summary>A follow-up is never drawn at random. Another decision queues it.</summary>
        public bool IsFollowUp { get; set; }

        /// <summary>An event elsewhere in the game raises it, about one subject such as a holding. It is never drawn at random.</summary>
        public bool IsEventDriven { get; set; }

        /// <summary>When it arrives, the secret entry it carries is exposed in the Gazette.</summary>
        public bool ExposesSecret { get; set; }

        public List<SituationRequirement> Requirements { get; }

        public List<SituationChoice> Choices { get; }

        /// <summary>What happens when nobody decides in time.</summary>
        public List<SituationEffect> IgnoredEffects { get; }

        /// <summary>The Gazette entry when nobody decides.</summary>
        public string IgnoredGazetteText { get; set; }

        /// <summary>The ignored entry also goes to the DM feed as a story hook.</summary>
        public bool IgnoredIsHook { get; set; }
    }
}
