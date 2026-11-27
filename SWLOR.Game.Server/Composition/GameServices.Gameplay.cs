using SWLOR.Game.Server.Domain.Gameplay;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The runtime state of the server's scheduled tasks (the facade <c>Service.ServerTask</c> until it was removed).</summary>
        public static IServerTaskService ServerTask => FacadeAccessor.Resolve<IServerTaskService>();
    }
}
