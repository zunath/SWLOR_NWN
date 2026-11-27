using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>One option an official can pick for a situation.</summary>
    public sealed class SituationChoice
    {
        public SituationChoice(string text)
        {
            Text = text;
            GazetteText = string.Empty;
            Requirements = new List<SituationRequirement>();
            Effects = new List<SituationEffect>();
        }

        /// <summary>The button text.</summary>
        public string Text { get; }

        /// <summary>The Gazette entry for picking this choice, in the past tense.</summary>
        public string GazetteText { get; set; }

        /// <summary>The entry stays out of the public Gazette until a later situation exposes it.</summary>
        public bool IsSecret { get; set; }

        /// <summary>The entry also goes to the DM feed as a story hook.</summary>
        public bool IsHook { get; set; }

        public List<SituationRequirement> Requirements { get; }

        public List<SituationEffect> Effects { get; }

        /// <summary>The credits the treasury must hold for the choice to be picked. 0 for a free choice or a gain.</summary>
        public int Cost => SituationRules.GetCost(Effects);

        /// <summary>The credits the choice costs for a situation whose subject owes <paramref name="subjectCredits"/>, such as a customs fine.</summary>
        public int GetCost(int subjectCredits) => SituationRules.GetCost(Effects, subjectCredits);
    }
}
