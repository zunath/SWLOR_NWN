using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the factions. The logic and state live in <see cref="IFactionService"/>; every member here
    /// forwards to the container's instance, and the event handler stays here so its name in the handler order
    /// manifest does not change.
    /// </summary>
    public static class Faction
    {
        // Compile-time constants: callers (including the toolset) get the values inlined, as before.
        public const int MinimumFaction = Domain.Quests.FactionService.MinimumFaction;
        public const int MaximumFaction = Domain.Quests.FactionService.MaximumFaction;

        private static IFactionService Instance => FacadeAccessor.Resolve<IFactionService>();

        /// <summary>
        /// When the module caches, cache all faction details into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadFactions()
        {
            Instance.LoadFactions();
        }

        /// <summary>
        /// Retrieves all of the available factions registered in the system.
        /// </summary>
        /// <returns>A dictionary of all available factions registered in the system.</returns>
        public static Dictionary<FactionType, FactionAttribute> GetAllFactions()
        {
            return Instance.GetAllFactions();
        }

        /// <summary>
        /// Retrieves details about a particular faction.
        /// Will throw an exception if faction is not registered.
        /// </summary>
        /// <param name="factionType">The type of faction to retrieve.</param>
        /// <returns>A faction detail matching the type.</returns>
        public static FactionAttribute GetFactionDetail(FactionType factionType)
        {
            return Instance.GetFactionDetail(factionType);
        }

        /// <summary>
        /// Adjusts a player's faction standing by a certain amount.
        /// </summary>
        /// <param name="player">The player whose faction standing will be adjusted.</param>
        /// <param name="faction">The faction to adjust</param>
        /// <param name="adjustBy">The amount to adjust by. This can be positive for increases and negative for decreases.</param>
        public static void AdjustPlayerFactionStanding(uint player, FactionType faction, int adjustBy)
        {
            Instance.AdjustPlayerFactionStanding(player, faction, adjustBy);
        }

        /// <summary>
        /// Adds or removes points towards a particular faction on a player.
        /// Amount can be positive or negative.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="faction">The faction to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        public static void AdjustPlayerFactionPoints(uint player, FactionType faction, int adjustBy)
        {
            Instance.AdjustPlayerFactionPoints(player, faction, adjustBy);
        }
    }
}
