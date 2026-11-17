using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// What the loot service needs to know about a beast's corpse: the extractable corpse placeable and how it is named.
    /// Items owns the interface and <c>IBeastMasteryService</c> extends it (supply rule 2 of
    /// MaintainabilityRefactorDomainOrder.md; P5-15b).
    /// </summary>
    public interface IBeastCorpseInfo
    {
        /// <summary>The resref of the placeable left behind by a beast whose corpse was emptied.</summary>
        const string ExtractCorpseObjectResref = "extract_corpse";

        /// <summary>The local variable that holds the type of the beast a corpse belonged to.</summary>
        const string BeastTypeVariable = "BEAST_TYPE";

        /// <summary>The local variable that holds the level the beast died at.</summary>
        const string BeastLevelVariable = "BEAST_LEVEL";

        /// <summary>The name of a beast type, as the beast catalog lists it.</summary>
        string GetBeastName(BeastType type);
    }
}
