using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Loads and saves each DM's DM Creator favorites: the logic that used to be static in
    /// <c>Service.DMCreatorFavorite</c>, moved here unchanged. The facade forwards every call to the container's
    /// instance.
    /// </summary>
    public class DMCreatorFavoriteService : IDMCreatorFavoriteService
    {
        // The DB entity cache is keyed by id alone, and DMPreferences already uses "DM:" with the CD key.
        private const string IdPrefix = "DMCreatorFavorites:";

        private readonly IDBService _db;
        private readonly IDMPresenceService _presence;

        public DMCreatorFavoriteService(IDBService db, IDMPresenceService presence)
        {
            _db = db;
            _presence = presence;
        }

        public List<DMFavoriteBlueprint> Get(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return new List<DMFavoriteBlueprint>();

            return _db.Get<DMCreatorFavorites>(id)?.Blueprints ?? new List<DMFavoriteBlueprint>();
        }

        public DMFavoriteChange Toggle(uint actor, DMBlueprint blueprint, out List<DMFavoriteBlueprint> favorites)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
            {
                favorites = new List<DMFavoriteBlueprint>();
                return DMFavoriteChange.NotSaved;
            }

            // Read the saved list rather than the window's copy, so a second session on the same key is not overwritten.
            var record = _db.Get<DMCreatorFavorites>(id) ?? new DMCreatorFavorites { Id = id };
            var change = DMFavoriteList.Toggle(record.Blueprints, blueprint);
            if (change != DMFavoriteChange.Full)
                _db.Set(record);

            favorites = record.Blueprints;
            return change;
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
