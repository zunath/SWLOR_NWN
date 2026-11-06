using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Slicing: the register of the attempts in progress. The board rules are the static helper
    /// <c>SlicingService.Slicing</c>; the attempts are run through <c>SlicingService.SlicingSession</c>.
    /// </summary>
    public static class Slicing
    {
        private static ISlicingService Instance => FacadeAccessor.Resolve<ISlicingService>();

        /// <summary>The attempt in progress for the player id, or null.</summary>
        public static ActiveSlicingSession GetSession(string playerId)
        {
            return Instance.GetSession(playerId);
        }

        /// <summary>True when the player id has an attempt in progress.</summary>
        public static bool HasSession(string playerId)
        {
            return Instance.HasSession(playerId);
        }

        /// <summary>Registers the attempt in progress for the player id, replacing any earlier one.</summary>
        public static void SetSession(string playerId, ActiveSlicingSession session)
        {
            Instance.SetSession(playerId, session);
        }

        /// <summary>Forgets the attempt in progress for the player id. Does nothing when there is none.</summary>
        public static void RemoveSession(string playerId)
        {
            Instance.RemoveSession(playerId);
        }
    }
}
