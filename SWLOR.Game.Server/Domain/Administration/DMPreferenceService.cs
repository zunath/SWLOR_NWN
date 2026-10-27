using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Loads and saves DM chat and display preferences: the logic that used to be static in <c>Service.DMPreference</c>,
    /// moved here unchanged. The facade forwards every call to the container's instance.
    /// </summary>
    public class DMPreferenceService : IDMPreferenceService
    {
        // The DB entity cache is keyed by id alone, and Account already uses the bare CD key.
        private const string IdPrefix = "DM:";

        private readonly IDBService _db;
        private readonly IDMPresenceService _presence;

        public DMPreferenceService(IDBService db, IDMPresenceService presence)
        {
            _db = db;
            _presence = presence;
        }

        public DMPreferences Get(uint actor)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return new DMPreferences();

            return _db.Get<DMPreferences>(id) ?? new DMPreferences { Id = id };
        }

        public bool Update(uint actor, Action<DMPreferences> change)
        {
            var id = GetId(actor);
            if (string.IsNullOrEmpty(id))
                return false;

            var preferences = _db.Get<DMPreferences>(id) ?? new DMPreferences { Id = id };
            change(preferences);
            _db.Set(preferences);
            return true;
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
