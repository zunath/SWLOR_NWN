using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Windows and the NUI event routing (the facade <c>Service.Gui</c> until P2-ZJ).</summary>
        public static IGuiService Gui => FacadeAccessor.Resolve<IGuiService>();
    }
}
