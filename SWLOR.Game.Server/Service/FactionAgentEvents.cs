using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What counts toward faction agent missions out in space (plan section 29): kills in the named system, the escort
    /// convoy reaching its starlane, a lead's site cleared or its target data can sliced, and the courier load delivered.
    /// </summary>
    public static partial class FactionAgents
    {
        private const string ConvoyMissionVariable = "AGENT_CONVOY_MISSION";
        private const string ConvoyPlayerVariable = "AGENT_CONVOY_PLAYER";
        private const string HeistMissionVariable = "AGENT_HEIST_MISSION";
        private const string HeistPlayerVariable = "AGENT_HEIST_PLAYER";
        private const float GuardRingMeters = 10f;

        /// <summary>Escort convoys out for a mission, by player id and mission id. Not saved: a restart sends a new one.</summary>
        private static readonly Dictionary<(string PlayerId, string MissionId), uint> _convoys = new();

        /// <summary>Leads (signatures) placed for a mission, by player id and mission id.</summary>
        private static readonly Dictionary<(string PlayerId, string MissionId), string> _leads = new();

        // Kills

        /// <summary>
        /// A destroyed NPC ship counts for the kill missions of the killer's party in the system: the killing blow's
        /// pilot (else the top-threat player) and their party members in the same area.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void CreditKillMissions()
        {
            var creature = OBJECT_SELF;
            if (!Space.TryGetMissionKill(creature, out var tier, out var role))
                return;

            var killer = GetLastKiller();
            if (!GetIsPC(killer))
                killer = Enmity.GetHighestEnmityTarget(creature);
            if (!GetIsObjectValid(killer) || !GetIsPC(killer) || GetIsDM(killer))
                return;

            var area = GetArea(creature);
            var areaResref = GetResRef(area);
            foreach (var member in GetPartyInArea(killer, area))
            {
                foreach (var mission in GetMissions(member).Where(x => FactionAgentRules.IsKillCredit(x, areaResref, tier, role)).ToList())
                    Credit(member, mission.Id, creature);
            }
        }

        private static List<uint> GetPartyInArea(uint player, uint area)
        {
            var members = new List<uint>();
            for (var member = GetFirstFactionMember(player); GetIsObjectValid(member); member = GetNextFactionMember(player))
            {
                if (GetArea(member) == area && !GetIsDM(member))
                    members.Add(member);
            }

            if (!members.Contains(player))
                members.Add(player);
            return members;
        }

        // Arriving in a mission's system

        /// <summary>A pilot entering a mission's system meets the escort convoy or is shown the agent's lead.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void StartMissionsInSystem()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var area = OBJECT_SELF;
            var areaResref = GetResRef(area);
            if (TradeLookup.GetOrbit(areaResref) == null || !GetMissions(player).Any(x => x.OrbitResref == areaResref))
                return;

            DelayCommand(FactionAgentTuning.ConvoySpawnDelaySeconds, () => StartMissionsInSystem(player, area));
        }

        private static void StartMissionsInSystem(uint player, uint area)
        {
            if (!GetIsObjectValid(player) || GetArea(player) != area || !Space.IsPlayerInSpaceMode(player))
                return;

            var playerId = GetObjectUUID(player);
            var areaResref = GetResRef(area);
            foreach (var mission in GetMissions(player).Where(x => x.OrbitResref == areaResref).ToList())
            {
                switch (mission.Type)
                {
                    case AgentMissionType.ConvoyEscort:
                        if (!_convoys.TryGetValue((playerId, mission.Id), out var hauler) || !GetIsObjectValid(hauler) || GetIsDead(hauler))
                            StartEscort(player, area, mission);
                        break;
                    case AgentMissionType.ScanDown:
                    case AgentMissionType.DataHeist:
                        var signature = _leads.TryGetValue((playerId, mission.Id), out var id) ? Space.FindSignature(id) : null;
                        if (signature == null || signature.Completed)
                            PlaceLead(player, area, mission);
                        break;
                }
            }
        }

        // Convoy escort

        private static void StartEscort(uint player, uint area, AgentMission mission)
        {
            var angle = Random.NextFloat() * 360f;
            var position = GetPosition(player);
            var spot = Vector3(
                position.X + FactionAgentTuning.ConvoySpawnMeters * (float)Math.Cos(angle * Math.PI / 180),
                position.Y + FactionAgentTuning.ConvoySpawnMeters * (float)Math.Sin(angle * Math.PI / 180),
                position.Z);
            var hauler = Trade.StartEscortConvoy(Location(area, spot, angle), mission.Tier);
            if (!GetIsObjectValid(hauler))
                return;

            var playerId = GetObjectUUID(player);
            SetLocalString(hauler, ConvoyMissionVariable, mission.Id);
            SetLocalString(hauler, ConvoyPlayerVariable, playerId);
            _convoys[(playerId, mission.Id)] = hauler;
            SendMessageToPC(player, ColorToken.Green("Your convoy drops out of hyperspace nearby and turns for a starlane. Keep it alive until it jumps."));

            var tier = mission.Tier;
            DelayCommand(FactionAgentTuning.ConvoyRaiderDelaySeconds, () =>
            {
                if (!GetIsObjectValid(hauler) || GetIsDead(hauler))
                    return;

                var raiders = Trade.SpawnConvoyRaiders(hauler, tier, FactionAgentTuning.ConvoyRaiders[Math.Clamp(tier, 1, 6)], player);
                if (GetIsObjectValid(player) && raiders > 0)
                    SendMessageToPC(player, ColorToken.Red("Pirate raiders close on your convoy!"));
            });
            Log.Write(LogGroup.Space, $"AGENT CONVOY - {PlayerName.GetAuditName(player)} - {mission.Id} T{mission.Tier} in {GetResRef(area)}");
        }

        /// <summary>An escort convoy reached its starlane and jumped out: the escort mission is done.</summary>
        public static void OnConvoyArrived(uint hauler)
        {
            var missionId = GetLocalString(hauler, ConvoyMissionVariable);
            if (string.IsNullOrEmpty(missionId))
                return;

            var playerId = GetLocalString(hauler, ConvoyPlayerVariable);
            _convoys.Remove((playerId, missionId));
            var player = FindOnlinePlayer(playerId);
            if (!GetIsObjectValid(player))
                return;

            SendMessageToPC(player, ColorToken.Green("Your convoy jumps out safely."));
            Credit(player, missionId, hauler);
        }

        /// <summary>An escort convoy's hauler was destroyed: the escort mission fails.</summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void FailLostEscort()
        {
            var hauler = OBJECT_SELF;
            var missionId = GetLocalString(hauler, ConvoyMissionVariable);
            if (string.IsNullOrEmpty(missionId))
                return;

            var playerId = GetLocalString(hauler, ConvoyPlayerVariable);
            _convoys.Remove((playerId, missionId));
            var player = FindOnlinePlayer(playerId);
            if (GetIsObjectValid(player))
                FailMission(player, missionId, "Your convoy was destroyed. The agent's mission has failed.");
        }

        // Leads: scan-down and data heist

        private static void PlaceLead(uint player, uint area, AgentMission mission)
        {
            var position = GetPositionFromLocation(Walkmesh.GetRandomLocation(area));
            if (position.X == 0f && position.Y == 0f)
                return;

            var playerId = GetObjectUUID(player);
            var type = mission.Type == AgentMissionType.DataHeist ? SignatureType.DataSite : mission.Site;
            var signature = SignatureSiteRules.CreateAgentLead(Guid.NewGuid().ToString(), GetResRef(area), mission.Tier, type,
                position.X, position.Y, position.Z, DateTime.UtcNow, mission.Id, playerId);
            Space.PlaceSignature(signature);
            _leads[(playerId, mission.Id)] = signature.Id;

            SendMessageToPC(player, ColorToken.Cyan(mission.Type == AgentMissionType.DataHeist
                ? "Your agent's intel marks a guarded data site in this system. Scan it down with a Probe Launcher; only your party's probes can find it."
                : "Your agent marks a hidden signature in this system. Scan it down with a Probe Launcher; only your party's probes can find it."));
        }

        private static void RemoveLead(string playerId, string missionId)
        {
            if (!_leads.Remove((playerId, missionId), out var signatureId))
                return;

            var signature = Space.FindSignature(signatureId);
            if (signature != null)
                Space.RemoveSignature(signature);
        }

        /// <summary>
        /// A heist's data site opens: a squad of the mission tier holds it, and one of its cans is the target, marked for
        /// the mission's holder.
        /// </summary>
        public static void HoldHeistSite(ScanSignature signature, uint area, Vector3 center)
        {
            Space.SpawnSiteObjectives(signature, area, center, SignatureSiteRules.CombatSquad(signature.Tier, false), ObjectType.Creature, GuardRingMeters);
            var target = signature.Objectives.FirstOrDefault(x => GetIsObjectValid(x) && GetTag(x) == Space.DataCanTag);
            if (!GetIsObjectValid(target))
                return;

            SetName(target, "Target Data Can");
            SetLocalString(target, HeistMissionVariable, signature.AgentMissionId);
            SetLocalString(target, HeistPlayerVariable, signature.AgentPlayerId);
        }

        /// <summary>A data can was sliced: the target of a heist completes the mission for its holder in the slicer's party.</summary>
        public static void OnHeistTargetSliced(uint slicer, uint can)
        {
            var missionId = GetLocalString(can, HeistMissionVariable);
            if (string.IsNullOrEmpty(missionId))
                return;

            var playerId = GetLocalString(can, HeistPlayerVariable);
            var holder = FindOnlinePlayer(playerId);
            if (!GetIsObjectValid(holder) || !Space.GetScanPartyIds(slicer).Contains(playerId))
                return;

            Credit(holder, missionId, can);
        }

        /// <summary>A heist's target can was destroyed by a failed slice: a new lead comes on the next arrival.</summary>
        public static void OnHeistTargetLost(uint can)
        {
            var missionId = GetLocalString(can, HeistMissionVariable);
            if (string.IsNullOrEmpty(missionId))
                return;

            var playerId = GetLocalString(can, HeistPlayerVariable);
            var holder = FindOnlinePlayer(playerId);
            var signature = _leads.TryGetValue((playerId, missionId), out var id) ? Space.FindSignature(id) : null;
            var next = signature?.Objectives.FirstOrDefault(x => x != can && GetIsObjectValid(x) && GetTag(x) == Space.DataCanTag) ?? OBJECT_INVALID;
            if (GetIsObjectValid(next))
            {
                SetName(next, "Target Data Can");
                SetLocalString(next, HeistMissionVariable, missionId);
                SetLocalString(next, HeistPlayerVariable, playerId);
                if (GetIsObjectValid(holder))
                    SendMessageToPC(holder, ColorToken.Red("The target data can is lost. Your agent's intel points to another can at the site."));
                return;
            }

            _leads.Remove((playerId, missionId));
            if (GetIsObjectValid(holder))
                SendMessageToPC(holder, ColorToken.Red("The target data can is lost. Your agent will mark a new site the next time you enter the system."));
        }

        /// <summary>A site's cache was opened: a scan-down lead completes the mission for its holder in the opener's party.</summary>
        public static void OnSiteCompleted(ScanSignature signature, uint opener, uint cache)
        {
            if (signature == null || !signature.IsAgentLead || signature.Type == SignatureType.DataSite)
                return;

            var holder = FindOnlinePlayer(signature.AgentPlayerId);
            if (!GetIsObjectValid(holder) || !Space.GetScanPartyIds(opener).Contains(signature.AgentPlayerId))
                return;

            _leads.Remove((signature.AgentPlayerId, signature.AgentMissionId));
            Credit(holder, signature.AgentMissionId, cache);
        }

        // Sealed courier

        /// <summary>A courier run's load was delivered from the Freight tab: on time it completes and pays, late it fails.</summary>
        public static void OnCourierDelivered(uint player, string missionId, bool late)
        {
            if (late)
            {
                FailMission(player, missionId, "The sealed load arrived late. The agent's contact refuses it and the mission has failed.");
                return;
            }

            SendMessageToPC(player, ColorToken.Green("The sealed load is signed for."));
            Credit(player, missionId, OBJECT_INVALID);
        }

        /// <summary>A courier run's load was lost with its ship: the mission fails (offline, it is dropped from the record).</summary>
        public static void OnCourierLost(string playerId, string missionId)
        {
            var player = FindOnlinePlayer(playerId);
            if (GetIsObjectValid(player))
            {
                FailMission(player, missionId, "Your agent's sealed load was lost with your ship. The mission has failed.");
                return;
            }

            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || dbPlayer.AgentMissions.RemoveAll(x => x.Id == missionId) == 0)
                return;

            dbPlayer.Quests.Remove(missionId);
            DB.Set(dbPlayer);
            Log.Write(LogGroup.Space, $"AGENT COURIER LOST - player {playerId} - {missionId}");
        }
    }
}
