using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The extra hotbars (the facade <c>Service.ExtraHotbars</c> until P2-ZJ).</summary>
        public static IExtraHotbarsService ExtraHotbars => FacadeAccessor.Resolve<IExtraHotbarsService>();

        /// <summary>Windows and the NUI event routing (the facade <c>Service.Gui</c> until P2-ZJ).</summary>
        public static IGuiService Gui => FacadeAccessor.Resolve<IGuiService>();
    }
}
