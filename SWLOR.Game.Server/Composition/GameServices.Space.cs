using SWLOR.Game.Server.Domain.Space;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Ships, ship modules, space objects, docking and the ship combat tick (the static class <c>Space</c> until it was converted).</summary>
        public static ISpaceService Space => FacadeAccessor.Resolve<ISpaceService>();
    }
}
