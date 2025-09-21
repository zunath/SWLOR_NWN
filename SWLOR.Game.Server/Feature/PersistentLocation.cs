using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Feature
{
    public class PersistentLocation
    {
        private readonly IDatabaseService _db;

        public PersistentLocation(IDatabaseService db)
        {
            _db = db;
        }
        
        /// <summary>
        /// Saves a player's position to the database.
        /// Does nothing for NPCs and DMs.
        /// </summary>
        /// <param name="player">The player whose data will be stored to the database.</param>
        public void SaveLocation(uint player)
        {
            var area = GetArea(player);
            var areaResref = GetResRef(area);
            var isSpace = GetLocalBool(area, "SPACE") || GetName(area).StartsWith("Space -");

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player) || areaResref == "ooc_area" || isSpace || areaResref == "char_migration" || areaResref == "spending_area")
                return;

            // If the area isn't in the cache, it must be an instance. Don't save locations inside instances.
            if (Area.GetAreaByResref(areaResref) == OBJECT_INVALID) return;

            var position = GetPosition(player);
            var orientation = GetFacing(player);
            var playerId = GetObjectUUID(player);
            var entity = _db.Get<Player>(playerId) ?? new Player(playerId);

            entity.LocationX = position.X;
            entity.LocationY = position.Y;
            entity.LocationZ = position.Z;
            entity.LocationOrientation = orientation;
            entity.LocationAreaResref = GetResRef(area);

            _db.Set(entity);
        }

        /// <summary>
        /// Saves a player's location on area enter.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void SaveLocationOnAreaEnter()
        {
            var player = GetEnteringObject();
            SaveLocation(player);
        }

        /// <summary>
        /// Saves a player's location on rest.
        /// </summary>
        [ScriptHandler<OnModuleRest>]
        public void SaveLocationOnRest()
        {
            var player = GetLastPCRested();
            if (GetLastRestEventType() != RestEventType.Started) 
                return;
            
            SaveLocation(player);
        }

        /// <summary>
        /// Loads a player's location if they enter an area with the tag "ooc_area".
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
        public void LoadLocationOnEnter()
        {
            var player = GetEnteringObject();
            var area = GetArea(player);
            var areaTag = GetTag(area);

            // Must be a player entering the OOC entry area, otherwise we exit early.
            if (!GetIsPC(player) || GetIsDM(player) || areaTag != "ooc_area") return;

            LoadLocation(player);
        }

        /// <summary>
        /// Loads a player's persistent location from the database and jumps them there.
        /// Does not work for DMs or NPCs.
        /// </summary>
        /// <param name="player"></param>
        public void LoadLocation(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer == null)
                return;

            // Rebuilds - Send player to rebuild area if they didn't finish their rebuild.
            if (!dbPlayer.RebuildComplete)
            {
                var rebuildLocation = GetLocation(GetWaypointByTag("REBUILD_LANDING"));
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(rebuildLocation);
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(dbPlayer.LocationAreaResref)) return;

            var locationArea = Area.GetAreaByResref(dbPlayer.LocationAreaResref);
            var position = Vector3(dbPlayer.LocationX, dbPlayer.LocationY, dbPlayer.LocationZ);

            var location = Location(locationArea, position, dbPlayer.LocationOrientation);

            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(location);
            });
        }
    }
}
