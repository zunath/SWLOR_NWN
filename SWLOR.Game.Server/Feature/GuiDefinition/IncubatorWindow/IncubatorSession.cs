using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow
{
    /// <summary>
    /// What one incubator window carries between its refreshes: the serialized DNA and enzyme items placed in it, their colors,
    /// the beast the DNA is for, the incubator it belongs to and the stat totals. The view model creates it once and the
    /// incubator parts read and write it through the host.
    /// </summary>
    public class IncubatorSession
    {
        /// <summary>The icon an empty slot shows.</summary>
        public const string Blank = "Blank";

        public string DnaItem { get; set; }
        public string HydrolaseItem { get; set; }
        public string IsomeraseItem { get; set; }
        public string LyaseItem { get; set; }

        public EnzymeColorType LyaseColor { get; set; }
        public EnzymeColorType IsomeraseColor { get; set; }
        public EnzymeColorType HydrolaseColor { get; set; }

        public BeastType DnaType { get; set; }
        public string IncubatorPropertyId { get; set; }
        public bool ShowResistances { get; set; }

        public Dictionary<IncubationStatType, int> BaseStats { get; } = new();
        public Dictionary<IncubationStatType, int> StageStats { get; } = new();

        public void ClearStats()
        {
            DnaType = BeastType.Invalid;
            BaseStats.Clear();
            ResetPendingEnzymes();
        }

        public void ResetPendingEnzymes()
        {
            StageStats.Clear();
            LyaseColor = EnzymeColorType.Invalid;
            IsomeraseColor = EnzymeColorType.Invalid;
            HydrolaseColor = EnzymeColorType.Invalid;
        }
    }
}
