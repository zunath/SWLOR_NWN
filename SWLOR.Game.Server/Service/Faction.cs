using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Service
{
    public static class Faction
    {
        private static readonly Dictionary<FactionType, FactionAttribute> _factions = new Dictionary<FactionType, FactionAttribute>();
        public const int MinimumFaction = -5000;
        public const int MaximumFaction = 5000;

        /// <summary>
        /// When the module caches, cache all faction details into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadFactions()
        {
            var factionTypes = Enum.GetValues(typeof(FactionType)).Cast<FactionType>();
            foreach (var factionType in factionTypes)
            {
                // Skip over the invalid faction.
                if (factionType == FactionType.Invalid)
                    continue;

                var detail = factionType.GetAttribute<FactionType, FactionAttribute>();
                _factions[factionType] = detail;
            }
        }

        /// <summary>
        /// Retrieves all of the available factions registered in the system.
        /// </summary>
        /// <returns>A dictionary of all available factions registered in the system.</returns>
        public static Dictionary<FactionType, FactionAttribute> GetAllFactions()
        {
            return _factions.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves details about a particular faction.
        /// Will throw an exception if faction is not registered.
        /// </summary>
        /// <param name="factionType">The type of faction to retrieve.</param>
        /// <returns>A faction detail matching the type.</returns>
        public static FactionAttribute GetFactionDetail(FactionType factionType)
        {
            return _factions[factionType];
        }

        /// <summary>
        /// Adjusts a player's faction standing by a certain amount.
        /// </summary>
        /// <param name="player">The player whose faction standing will be adjusted.</param>
        /// <param name="faction">The faction to adjust</param>
        /// <param name="adjustBy">The amount to adjust by. This can be positive for increases and negative for decreases.</param>
        public static void AdjustPlayerFactionStanding(uint player, FactionType faction, int adjustBy)
        {
            if (!GetIsPC(player) || adjustBy == 0) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var factionDetail = _factions[faction];
            var cantGoHigher = false;
            var cantGoLower = false;

            if (!dbPlayer.Factions.ContainsKey(faction))
            {
                dbPlayer.Factions[faction] = new PlayerFactionStanding();
            }

            dbPlayer.Factions[faction].Standing += adjustBy;

            // Clamp faction standing range.
            if (dbPlayer.Factions[faction].Standing > MaximumFaction)
            {
                dbPlayer.Factions[faction].Standing = MaximumFaction;
                cantGoHigher = true;
            }
            else if (dbPlayer.Factions[faction].Standing < MinimumFaction)
            {
                dbPlayer.Factions[faction].Standing = MinimumFaction;
                cantGoLower = true;
            }

            if (adjustBy > 0)
            {
                if (cantGoHigher)
                {
                    SendMessageToPC(player, $"Your standing with {factionDetail.Name} cannot possibly go higher!");
                }
                else
                {
                    SendMessageToPC(player, $"Your standing with {factionDetail.Name} improves.");
                }

            }
            else
            {
                if (cantGoLower)
                {
                    SendMessageToPC(player, $"Your standing with {factionDetail.Name} cannot possibly go lower!");
                }
                else
                {
                    SendMessageToPC(player, $"Your standing with {factionDetail.Name} decreases.");
                }
            }

            DB.Set(dbPlayer);
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
            if (!GetIsPC(player) || adjustBy == 0) return;


            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var factionDetail = Faction.GetFactionDetail(faction);

            if (!dbPlayer.Factions.ContainsKey(faction))
            {
                dbPlayer.Factions[faction] = new PlayerFactionStanding();
            }
            
            dbPlayer.Factions[faction].Points += adjustBy;
            if (dbPlayer.Factions[faction].Points < 0)
                dbPlayer.Factions[faction].Points = 0;

            DB.Set(dbPlayer);

            if (adjustBy > 0)
            {
                SendMessageToPC(player, $"You gained {adjustBy} points with the {factionDetail.Name} faction.");
            }
            else
            {
                SendMessageToPC(player, $"You lost {Math.Abs(adjustBy)} points with the {factionDetail.Name} faction.");
            }

        }
    }
}
