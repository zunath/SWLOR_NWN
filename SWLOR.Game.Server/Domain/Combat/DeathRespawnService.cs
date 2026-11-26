using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Where a player respawns and what dying costs: the respawn point set on login, the location a respawn resolves to, the
    /// jump to the home point, the XP debt penalty and the audit lines. These members moved from <see cref="DeathService"/>
    /// unchanged; the dying, death and respawn handlers stay there and call this part. The call into the unconverted
    /// <c>Log</c> sits behind a one-line <c>protected virtual</c> method whose body is the original call.
    /// </summary>
    public class DeathRespawnService : IDeathRespawnService
    {
        private const string DefaultRespawnWaypointTag = "DTH_DEFAULT_RESPAWN_POINT";

        /// <summary>The local variable that records a death-debt waiver claimed at death and spent at respawn.</summary>
        public const string DebtWaivedVariable = "DEATH_DEBT_WAIVED";

        private readonly IDBService _db;
        private readonly IAreaLookup _area;
        private readonly ICityUpgradeLookup _property;
        private readonly IPlayerNameService _playerName;

        public DeathRespawnService(
            IDBService db,
            IAreaLookup area,
            ICityUpgradeLookup property,
            IPlayerNameService playerName)
        {
            _db = db;
            _area = area;
            _property = property;
            _playerName = playerName;
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "Writes a missing respawn point to Redis; the login already did.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void InitializeRespawnPoint()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

            // Already have a respawn point, no need to set the default one.
            if (HasRespawnArea(dbPlayer)) return;

            var waypoint = GetWaypointByTag(DefaultRespawnWaypointTag);
            var position = GetPosition(waypoint);
            var areaResref = GetResRef(GetArea(waypoint));
            var facing = GetFacing(waypoint);

            dbPlayer.RespawnLocationX = position.X;
            dbPlayer.RespawnLocationY = position.Y;
            dbPlayer.RespawnLocationZ = position.Z;
            dbPlayer.RespawnAreaResref = areaResref;
            dbPlayer.RespawnLocationOrientation = facing;

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Write an audit entry with details of this death.
        /// </summary>
        /// <param name="player">The player who died</param>
        public void WriteAudit(uint player)
        {
            var name = _playerName.GetAuditName(player);
            var area = GetArea(player);
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var hostile = GetLastHostileActor(player);
            var hostileName = _playerName.GetAuditName(hostile);

            var log = $"DEATH: {name} - {areaName} - {areaTag} - {areaResref} Killed by: {hostileName}";
            WriteLog(LogGroup.Death, log);
        }

        /// <summary>
        /// Teleports player to his or her last home point.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        public void SendToHomePoint(uint player)
        {
            var playerId = GetObjectUUID(player);
            var destination = GetRespawnLocation(_db.Get<Player>(playerId));

            // Jump immediately rather than queueing an action: resurrection and the death
            // state clear the action queue, which left players respawning where they died.
            AssignCommand(player, () =>
            {
                ClearAllActions(true);
                JumpToLocation(destination);
            });
        }

        public Location GetRespawnLocation(Player dbPlayer)
        {
            if (HasRespawnArea(dbPlayer))
            {
                var area = _area.GetAreaByResref(dbPlayer.RespawnAreaResref);
                if (GetIsObjectValid(area))
                {
                    var position = Vector3(
                        dbPlayer.RespawnLocationX,
                        dbPlayer.RespawnLocationY,
                        dbPlayer.RespawnLocationZ);
                    var location = Location(area, position, dbPlayer.RespawnLocationOrientation);

                    if (IsWalkable(location))
                        return location;
                }
            }

            return GetLocation(GetWaypointByTag(DefaultRespawnWaypointTag));
        }

        public bool HasRespawnArea(Player dbPlayer)
        {
            return !string.IsNullOrWhiteSpace(dbPlayer?.RespawnAreaResref);
        }

        private static bool IsWalkable(Location location)
        {
            var material = GetSurfaceMaterial(location);
            return material > 0 &&
                   int.TryParse(Get2DAString("surfacemat", "Walk", material), out var walkable) &&
                   walkable == 1;
        }

        /// <summary>
        /// Applies death penalties for a player.
        /// </summary>
        /// <param name="player">The player who we're applying penalties to</param>
        public int ApplyPenalties(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (GetLocalInt(player, DebtWaivedVariable) == 1)
            {
                DeleteLocalInt(player, DebtWaivedVariable);
                SendMessageToPC(player, "Requiem spares you any XP debt.");
                return dbPlayer.XPDebt;
            }

            int multiplier;

            // 300+
            if (dbPlayer.TotalSPAcquired >= 300)
                multiplier = 45;
            // 200 - 299
            else if (dbPlayer.TotalSPAcquired >= 200)
                multiplier = 35;
            // 50 - 199
            else if (dbPlayer.TotalSPAcquired >= 50)
                multiplier = 25;
            // 0 - 49
            else
                multiplier = 15;

            var social = GetAbilityScore(player, AbilityType.Social);
            var newDebt = dbPlayer.TotalSPAcquired * multiplier;
            var reductionBonus = 0f;
            reductionBonus += _property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.MedicalCenterLevel) * 0.05f; // -5% per Medical Center level

            if (social > 10)
            {
                reductionBonus += (social - 10) * 0.03f; // -3% per SOC
            }

            if (reductionBonus > 0.8f)
                reductionBonus = 0.8f;

            newDebt -= (int)(newDebt * reductionBonus);

            dbPlayer.XPDebt += newDebt;

            const int MaxDebt = 9999999;
            if (dbPlayer.XPDebt > MaxDebt)
                dbPlayer.XPDebt = MaxDebt;

            _db.Set(dbPlayer);

            SendMessageToPC(player, $"{newDebt} XP added to your debt. (Total: {dbPlayer.XPDebt} XP)");

            return dbPlayer.XPDebt;
        }

        /// <summary>
        /// Writes an audit entry to the Death audit group.
        /// </summary>
        /// <param name="player">The player who respawned</param>
        /// <param name="xpLost">The amount of XP lost</param>
        public void WriteAudit(uint player, int xpLost)
        {
            var name = _playerName.GetAuditName(player);
            var log = $"RESPAWN - {name} - {xpLost} XP lost";

            WriteLog(LogGroup.Death, log);
        }

        protected virtual void WriteLog(LogGroup group, string details) => Log.Write(group, details);
    }
}
