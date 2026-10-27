using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of overflow storage: items a character upgrade could not keep in a character's inventory. The
    /// logic lives in <see cref="IOverflowStorageService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class OverflowStorage
    {
        private static IOverflowStorageService Instance => FacadeAccessor.Resolve<IOverflowStorageService>();

        public static long GetItemCount(string playerId)
        {
            return Instance.GetItemCount(playerId);
        }

        /// <summary>Moves overflow items into the player's inventory until it is full.</summary>
        public static void Claim(uint player)
        {
            Instance.Claim(player);
        }

        /// <summary>Tells the player at login how many items are waiting in overflow storage.</summary>
        public static void NotifyOnLogin(uint player)
        {
            Instance.NotifyOnLogin(player);
        }
    }
}
