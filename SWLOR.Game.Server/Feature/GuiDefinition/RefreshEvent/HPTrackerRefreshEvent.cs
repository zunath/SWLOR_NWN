using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Signals the HP Tracker window to rebuild its list (HP changed, or creatures moved in/out of range).
    /// Kept separate from <c>PlayerStatusRefreshEvent</c>: that event is the real-vitals contract (HP/FP/STM
    /// and ship stats, consumed by the player status and character sheet windows), so sharing it would
    /// rebuild the HP Tracker on every combat vitals publish and vice versa.
    /// </summary>
    public class HPTrackerRefreshEvent : IGuiRefreshEvent
    {
    }
}
