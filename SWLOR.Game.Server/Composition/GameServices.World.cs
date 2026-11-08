using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Taxi destinations and the ones a player has registered (the facade <c>Service.Taxi</c> until P2-ZA).</summary>
        public static ITaxiService Taxi => FacadeAccessor.Resolve<ITaxiService>();
    }
}
