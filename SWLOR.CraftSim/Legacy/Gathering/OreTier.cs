using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>One ore the refinery turns into a refined item, with the XP it grants per unit.</summary>
    public sealed record OreTier(string Ore, string RefinedItem, int RequiredLevel, int XpPerUnit);
}
