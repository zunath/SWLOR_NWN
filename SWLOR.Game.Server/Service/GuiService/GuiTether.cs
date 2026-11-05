using System.Collections.Generic;
namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// The pure part of the tethered window check: which of a player's windows hold a tether object and are open.
    /// </summary>
    public static class GuiTether
    {
        public static bool HasOpenTetheredWindow(IEnumerable<(GuiWindowType Type, uint Tether)> windows,
            uint tetherObject, Func<GuiWindowType, bool> isWindowOpen)
        {
            if (windows == null || tetherObject == OBJECT_INVALID)
                return false;

            foreach (var (type, tether) in windows)
            {
                if (tether == tetherObject && isWindowOpen(type))
                    return true;
            }

            return false;
        }
    }
}
