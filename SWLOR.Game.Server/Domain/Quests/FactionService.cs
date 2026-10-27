using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Domain.Quests
{
    /// <summary>
    /// Factions: the logic and state that used to be static in <c>Service.Faction</c>, moved here unchanged. The facade
    /// still carries the event handler and forwards every call to the container's instance.
    /// </summary>
    public class FactionService : IFactionService
    {
        /// <summary>The lowest standing a player can have with a faction. <c>Service.Faction.MinimumFaction</c> repeats it.</summary>
        public const int MinimumFaction = -5000;

        /// <summary>The highest standing a player can have with a faction. <c>Service.Faction.MaximumFaction</c> repeats it.</summary>
        public const int MaximumFaction = 5000;

        private readonly IDBService _db;
        private readonly Dictionary<FactionType, FactionAttribute> _factions = new Dictionary<FactionType, FactionAttribute>();

        public FactionService(IDBService db)
        {
            _db = db;
        }

        public void LoadFactions()
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

        public Dictionary<FactionType, FactionAttribute> GetAllFactions()
        {
            return _factions.ToDictionary(x => x.Key, y => y.Value);
        }

        public FactionAttribute GetFactionDetail(FactionType factionType)
        {
            return _factions[factionType];
        }

        public void AdjustPlayerFactionStanding(uint player, FactionType faction, int adjustBy)
        {
            if (!GetIsPC(player) || adjustBy == 0) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
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

            _db.Set(dbPlayer);
        }

        public void AdjustPlayerFactionPoints(uint player, FactionType faction, int adjustBy)
        {
            if (!GetIsPC(player) || adjustBy == 0) return;


            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var factionDetail = GetFactionDetail(faction);

            if (!dbPlayer.Factions.ContainsKey(faction))
            {
                dbPlayer.Factions[faction] = new PlayerFactionStanding();
            }

            dbPlayer.Factions[faction].Points += adjustBy;
            if (dbPlayer.Factions[faction].Points < 0)
                dbPlayer.Factions[faction].Points = 0;

            _db.Set(dbPlayer);

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
