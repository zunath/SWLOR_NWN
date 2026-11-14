using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Settles holdings at the moment the Industrial Charter's effect changes (Player City Revamp, "Planetary industry"). A holding
    /// settles lazily over elapsed days, so the days that passed before a change must be advanced under the old state, and only
    /// then may the state change. Each place that enacts, repeals, pauses or resumes the Edict, or changes a citizenship or a tax
    /// debt, calls here before it changes anything. <see cref="EdictEffects.AdvanceHolding"/> reads the state that follows.
    /// </summary>
    public static class IndustrialCharter
    {
        /// <summary>Whether the city holds the Edict in any state: enacted, waiting, or paused.</summary>
        public static bool Holds(WorldProperty city)
        {
            return city != null && city.Edicts.ContainsKey(EdictType.IndustrialCharter);
        }

        /// <summary>
        /// Advances the holdings of the city's citizens to now, under the Charter as it stands. Does nothing for a city that does not
        /// hold the Edict, unless <paramref name="aboutToEnact"/> says it is about to.
        /// </summary>
        public static void SettleCity(WorldProperty city, bool aboutToEnact = false)
        {
            if (city == null || (!aboutToEnact && !Holds(city)))
                return;

            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return;

            foreach (var citizen in DB.Search(query.AddPaging(count, 0)).ToList())
            {
                PlanetaryHoldings.SettleHoldings(citizen.Id);
            }
        }

        public static void SettleCity(string cityId, bool aboutToEnact = false)
        {
            if (!string.IsNullOrWhiteSpace(cityId))
                SettleCity(DB.Get<WorldProperty>(cityId), aboutToEnact);
        }

        /// <summary>
        /// Advances one character's holdings to now, before their citizenship or tax debt changes. Does nothing without holdings.
        /// </summary>
        public static void SettleOwner(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId))
                PlanetaryHoldings.SettleHoldings(playerId);
        }
    }
}
