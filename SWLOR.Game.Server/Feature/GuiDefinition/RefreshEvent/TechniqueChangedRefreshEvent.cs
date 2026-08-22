using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Raised when a player's set of equipped techniques changes, since equipping or unequipping
    /// one can change loadout-derived stats, resistances, or elemental resonance.
    /// </summary>
    public class TechniqueChangedRefreshEvent: IGuiRefreshEvent
    {
    }
}
