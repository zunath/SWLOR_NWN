using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the per-player window table of the server's GUI instance. A headless test player has no client to log in
    /// through, so the engine tests register its windows by hand, as they did on the static class before the GUI
    /// service was converted.
    /// </summary>
    public static class GuiInternals
    {
        /// <summary>The live per-player window table (player id, window type) of the container's GUI service.</summary>
        public static Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> PlayerWindows()
        {
            var gui = CompositionRoot.ServerHost.Provider.GetService(typeof(IGuiService));
            return (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)gui.GetType()
                .GetField("_playerWindows", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(gui)!;
        }
    }
}
