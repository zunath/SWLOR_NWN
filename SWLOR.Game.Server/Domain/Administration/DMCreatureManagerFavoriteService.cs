using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Loads and saves each DM's Creature Manager favorites: the logic that used to be static in
    /// <c>Service.DMCreatureManagerFavorite</c>, moved here unchanged. The facade forwards every call to the
    /// container's instance.
    /// </summary>
    public class DMCreatureManagerFavoriteService : IDMCreatureManagerFavoriteService
    {
        // The DB entity cache is keyed by id alone, so each per-DM record type needs its own prefix.
        private const string IdPrefix = "DMCreatureManagerFavorites:";

        private readonly IDBService _db;
        private readonly IDMPresenceService _presence;

        public DMCreatureManagerFavoriteService(IDBService db, IDMPresenceService presence)
        {
            _db = db;
            _presence = presence;
        }

        public List<DMSavedCreatureFavorite> Get(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return new List<DMSavedCreatureFavorite>();

            return _db.Get<DMCreatureManagerFavorites>(id)?.Creatures ?? new List<DMSavedCreatureFavorite>();
        }

        public DMFavoriteChange Set(uint actor, string creatureId, string name, bool isFavorite, out List<DMSavedCreatureFavorite> favorites)
        {
            var record = GetRecord(actor);
            if (record == null)
            {
                favorites = new List<DMSavedCreatureFavorite>();
                return DMFavoriteChange.NotSaved;
            }

            // Read the saved list rather than the window's copy, so a second session on the same key is not overwritten.
            var change = DMSavedCreatureFavoriteList.Set(record.Creatures, creatureId, name, isFavorite);
            if (change != DMFavoriteChange.Full)
                _db.Set(record);

            favorites = record.Creatures;
            return change;
        }

        public List<DMSavedCreatureFavorite> Remove(uint actor, string creatureId)
        {
            var record = GetRecord(actor);
            if (record == null)
                return new List<DMSavedCreatureFavorite>();

            if (DMSavedCreatureFavoriteList.Remove(record.Creatures, creatureId))
                _db.Set(record);

            return record.Creatures;
        }

        private DMCreatureManagerFavorites GetRecord(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return null;

            return _db.Get<DMCreatureManagerFavorites>(id) ?? new DMCreatureManagerFavorites { Id = id };
        }

        private string GetId(uint actor)
        {
            // The CD key resolves from the object the player controls, which is the possessed creature while possessing.
            var cdKey = GetPCPublicCDKey(_presence.GetControlledCreature(actor));
            return string.IsNullOrWhiteSpace(cdKey)
                ? string.Empty
                : IdPrefix + cdKey;
        }
    }
}
