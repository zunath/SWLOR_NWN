using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service.QuestService
{
    public static class QuestEncounter
    {
        public const string EncounterIdVariable = "QUEST_ENCOUNTER_ID";
        public const string CreatureResrefVariable = "QUEST_ENCOUNTER_RESREF";
        public const string SpawnWaypointVariable = "QUEST_ENCOUNTER_WAYPOINT";
        public const string CooldownMinutesVariable = "QUEST_ENCOUNTER_COOLDOWN_MINUTES";
        public const string IdleDespawnMinutesVariable = "QUEST_ENCOUNTER_IDLE_MINUTES";
        public const string ActiveCreatureVariable = "QUEST_ENCOUNTER_ACTIVE";
        public const string ActivatorVariable = "QUEST_ENCOUNTER_ACTIVATOR";
        public const string EncounterCreatureFlagVariable = "QUEST_ENCOUNTER_CREATURE";

        private const int DefaultCooldownMinutes = 60;
        private const int DefaultIdleDespawnMinutes = 10;
        private const float ParticipantCreditRange = 50.0f;

        private static readonly Dictionary<string, uint> _activeCreaturesByEncounterId = new();
        private static readonly Dictionary<uint, DateTime> _lastActivityByCreature = new();

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            DelayCommand(1.0f, () => RefreshVisibilityForPlayer(player));
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            DelayCommand(0.2f, () => RefreshVisibilityForPlayer(player));
        }

        [NWNEventHandler(ScriptName.OnQuestEncounter)]
        public static void UseActivator()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var activator = OBJECT_SELF;

            if (!CanSeeActivator(player, activator))
            {
                SendMessageToPC(player, "You are not ready to use this object.");
                return;
            }

            var encounterId = GetEncounterId(activator);
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                Log.Write(LogGroup.Error, $"Quest encounter activator {GetActivatorLogContext(activator)} is missing its encounter ID.");
                SendMessageToPC(player, "This encounter object is missing its encounter ID. Please inform an admin.");
                return;
            }

            if (TryGetActiveCreature(activator, encounterId, out _))
            {
                SendMessageToPC(player, "This encounter is already active.");
                return;
            }

            var dbPlayer = GetPlayer(player);
            var remainingCooldown = GetCooldownRemaining(dbPlayer, encounterId, DateTime.UtcNow);
            if (remainingCooldown > TimeSpan.Zero)
            {
                SendMessageToPC(player, $"You need {Time.GetTimeLongIntervals(remainingCooldown, false)} before you can call this encounter again.");
                return;
            }

            var creatureResref = GetLocalString(activator, CreatureResrefVariable);
            if (string.IsNullOrWhiteSpace(creatureResref))
            {
                Log.Write(LogGroup.Error, $"Quest encounter '{encounterId}' activator {GetActivatorLogContext(activator)} is missing its creature resref.");
                SendMessageToPC(player, "This encounter object is missing its creature resref. Please inform an admin.");
                return;
            }

            var encounterCreature = CreateObject(ObjectType.Creature, creatureResref, GetSpawnLocation(activator), true);
            if (!GetIsObjectValid(encounterCreature))
            {
                Log.Write(LogGroup.Error, $"Quest encounter '{encounterId}' failed to spawn creature resref '{creatureResref}' from activator {GetActivatorLogContext(activator)}.");
                SendMessageToPC(player, "The encounter failed to start. Please inform an admin.");
                return;
            }

            SetLocalObject(activator, ActiveCreatureVariable, encounterCreature);
            _activeCreaturesByEncounterId[encounterId] = encounterCreature;
            SetLocalObject(encounterCreature, ActivatorVariable, activator);
            SetLocalBool(encounterCreature, EncounterCreatureFlagVariable, true);
            SetLocalString(encounterCreature, EncounterIdVariable, encounterId);
            MarkEncounterActivity(encounterCreature);

            StartCooldown(dbPlayer, encounterId, GetCooldown(activator), DateTime.UtcNow);
            Enmity.ModifyEnmity(player, encounterCreature, 1);
            AssignCommand(encounterCreature, () => ActionAttack(player));

            SendMessageToPC(player, "The encounter has begun.");
            RefreshVisibilityForArea(GetArea(activator));
            ScheduleIdleDespawnCheck(activator, encounterCreature, encounterId);
        }

        [NWNEventHandler(ScriptName.OnCreatureAttackAfter)]
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void TrackEncounterActivity()
        {
            var encounterCreature = OBJECT_SELF;
            if (!IsQuestEncounterCreature(encounterCreature)) return;

            MarkEncounterActivity(encounterCreature);
        }

        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void ObjectDestroyed()
        {
            var encounterCreature = OBJECT_SELF;
            if (!IsQuestEncounterCreature(encounterCreature)) return;

            ClearEncounterForCreature(encounterCreature);
        }

        public static bool IsQuestEncounterCreature(uint encounterCreature)
        {
            return GetLocalBool(encounterCreature, EncounterCreatureFlagVariable);
        }

        public static void ProgressKillCredit(uint encounterCreature, NPCGroupType npcGroupType, IReadOnlyCollection<string> possibleQuests)
        {
            var participants = GetParticipantPlayers(encounterCreature);

            foreach (var participant in participants)
            {
                ProgressKillCredit(participant, encounterCreature, npcGroupType, possibleQuests);
            }
        }

        public static bool IsPlayerOnQuestState(Player dbPlayer, string questId, int questState)
        {
            if (dbPlayer == null) return false;
            if (!dbPlayer.Quests.TryGetValue(questId, out var quest)) return false;

            return quest.CurrentState == questState && quest.TimesCompleted <= 0;
        }

        public static TimeSpan GetCooldownRemaining(Player dbPlayer, string encounterId, DateTime now)
        {
            if (dbPlayer?.EncounterCooldowns == null)
                return TimeSpan.Zero;

            if (!dbPlayer.EncounterCooldowns.TryGetValue(encounterId, out var availableAt))
                return TimeSpan.Zero;

            return availableAt > now ? availableAt - now : TimeSpan.Zero;
        }

        public static bool IsIdleExpired(DateTime lastActivity, DateTime now, TimeSpan idleTimeout)
        {
            return now - lastActivity >= idleTimeout;
        }

        public static void ClearEncounterForCreature(uint encounterCreature)
        {
            if (!IsQuestEncounterCreature(encounterCreature)) return;

            var activator = GetLocalObject(encounterCreature, ActivatorVariable);
            var encounterId = GetLocalString(encounterCreature, EncounterIdVariable);
            ClearActiveCreature(activator, encounterCreature, encounterId);
        }

        public static void RefreshVisibilityForPlayer(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var area = GetArea(player);
            if (!GetIsObjectValid(area)) return;

            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (!IsQuestEncounterActivator(obj))
                    continue;

                RefreshVisibilityForActivator(player, obj);
            }
        }

        public static void RefreshVisibilityForArea(uint area)
        {
            if (!GetIsObjectValid(area)) return;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) != area)
                    continue;

                RefreshVisibilityForPlayer(player);
            }
        }

        private static void ProgressKillCredit(
            uint player,
            uint encounterCreature,
            NPCGroupType npcGroupType,
            IReadOnlyCollection<string> possibleQuests)
        {
            if (!GetIsPC(player) ||
                GetIsDM(player) ||
                GetIsDead(player) ||
                GetCurrentHitPoints(player) <= 0)
                return;
            if (GetArea(player) != GetArea(encounterCreature)) return;
            if (GetDistanceBetween(player, encounterCreature) > ParticipantCreditRange) return;

            var dbPlayer = GetPlayer(player);

            foreach (var questId in possibleQuests)
            {
                if (!dbPlayer.Quests.TryGetValue(questId, out var playerQuest)) continue;

                var questDetail = Quest.GetQuestById(questId);
                if (!questDetail.States.TryGetValue(playerQuest.CurrentState, out var questState)) continue;

                var killRequiredForQuestAndState = false;
                foreach (var objective in questState.GetObjectives())
                {
                    if (objective is not KillTargetObjective killTargetObjective ||
                        killTargetObjective.Group != npcGroupType)
                    {
                        continue;
                    }

                    killRequiredForQuestAndState = true;
                    killTargetObjective.Advance(player, questId);
                }

                if (killRequiredForQuestAndState)
                {
                    questDetail.Advance(player, encounterCreature);
                }
            }
        }

        private static IReadOnlyList<uint> GetParticipantPlayers(uint encounterCreature)
        {
            var participants = new HashSet<uint>();
            foreach (var target in Enmity.GetEnmityTable(encounterCreature).Keys)
            {
                AddParticipantPlayer(participants, target);
            }

            if (participants.Count <= 0)
            {
                AddParticipantPlayer(participants, GetNearestCreature(CreatureType.PlayerCharacter, 1, encounterCreature));
            }

            foreach (var participant in participants.ToArray())
            {
                for (var member = GetFirstFactionMember(participant); GetIsObjectValid(member); member = GetNextFactionMember(participant))
                {
                    AddParticipantPlayer(participants, member);
                }
            }

            return participants.ToList();
        }

        private static void AddParticipantPlayer(HashSet<uint> participants, uint creature)
        {
            if (!GetIsObjectValid(creature)) return;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                participants.Add(creature);
                return;
            }

            var master = GetMaster(creature);
            if (GetIsPC(master) && !GetIsDM(master))
            {
                participants.Add(master);
            }
        }

        private static bool CanSeeActivator(uint player, uint activator)
        {
            var questId = GetLocalString(activator, "QUEST_ID");
            var questState = GetLocalInt(activator, "QUEST_STATE");

            if (string.IsNullOrWhiteSpace(questId) || questState <= 0)
                return false;

            return IsPlayerOnQuestState(GetPlayer(player), questId, questState);
        }

        private static void RefreshVisibilityForActivator(uint player, uint activator)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var encounterId = GetEncounterId(activator);
            var visibility = !TryGetActiveCreature(activator, encounterId, out _) &&
                             CanSeeActivator(player, activator)
                ? VisibilityType.Visible
                : VisibilityType.Hidden;

            VisibilityPlugin.SetVisibilityOverride(player, activator, visibility);
        }

        private static bool IsQuestEncounterActivator(uint obj)
        {
            return GetObjectType(obj) == ObjectType.Placeable &&
                   !string.IsNullOrWhiteSpace(GetLocalString(obj, EncounterIdVariable));
        }

        private static string GetActivatorLogContext(uint activator)
        {
            return $"'{GetName(activator)}' ({GetTag(activator)} / {GetResRef(activator)})";
        }

        private static Location GetSpawnLocation(uint activator)
        {
            var waypointTag = GetLocalString(activator, SpawnWaypointVariable);
            if (!string.IsNullOrWhiteSpace(waypointTag))
            {
                var waypoint = GetWaypointByTag(waypointTag);
                if (GetIsObjectValid(waypoint))
                {
                    return GetLocation(waypoint);
                }
            }

            return GetLocation(activator);
        }

        private static bool TryGetActiveCreature(uint activator, string encounterId, out uint encounterCreature)
        {
            encounterCreature = GetLocalObject(activator, ActiveCreatureVariable);
            if (GetIsObjectValid(encounterCreature) && !GetIsDead(encounterCreature))
                return true;

            DeleteLocalObject(activator, ActiveCreatureVariable);

            if (string.IsNullOrWhiteSpace(encounterId))
                return false;

            if (!_activeCreaturesByEncounterId.TryGetValue(encounterId, out encounterCreature))
                return false;

            if (GetIsObjectValid(encounterCreature) && !GetIsDead(encounterCreature))
                return true;

            _activeCreaturesByEncounterId.Remove(encounterId);
            return false;
        }

        private static void ClearActiveCreature(uint activator, uint encounterCreature, string encounterId = "")
        {
            if (GetIsObjectValid(activator) && GetLocalObject(activator, ActiveCreatureVariable) == encounterCreature)
            {
                DeleteLocalObject(activator, ActiveCreatureVariable);
            }

            if (string.IsNullOrWhiteSpace(encounterId) && GetIsObjectValid(encounterCreature))
                encounterId = GetLocalString(encounterCreature, EncounterIdVariable);

            if (!string.IsNullOrWhiteSpace(encounterId) &&
                _activeCreaturesByEncounterId.TryGetValue(encounterId, out var activeCreature) &&
                activeCreature == encounterCreature)
            {
                _activeCreaturesByEncounterId.Remove(encounterId);
            }

            _lastActivityByCreature.Remove(encounterCreature);

            if (GetIsObjectValid(activator))
                RefreshVisibilityForArea(GetArea(activator));
        }

        private static void StartCooldown(Player dbPlayer, string encounterId, TimeSpan cooldown, DateTime now)
        {
            dbPlayer.EncounterCooldowns ??= new Dictionary<string, DateTime>();
            dbPlayer.EncounterCooldowns[encounterId] = now.Add(cooldown);
            DB.Set(dbPlayer);
        }

        private static Player GetPlayer(uint player)
        {
            return DB.Get<Player>(GetObjectUUID(player)) ?? new Player(GetObjectUUID(player));
        }

        private static string GetEncounterId(uint activator)
        {
            var encounterId = GetLocalString(activator, EncounterIdVariable);
            if (!string.IsNullOrWhiteSpace(encounterId))
                return encounterId;

            var questId = GetLocalString(activator, "QUEST_ID");
            var questState = GetLocalInt(activator, "QUEST_STATE");
            return string.IsNullOrWhiteSpace(questId) || questState <= 0
                ? string.Empty
                : $"{questId}:{questState}";
        }

        private static TimeSpan GetCooldown(uint activator)
        {
            var minutes = GetLocalInt(activator, CooldownMinutesVariable);
            if (minutes <= 0)
                minutes = DefaultCooldownMinutes;

            return TimeSpan.FromMinutes(minutes);
        }

        private static TimeSpan GetIdleTimeout(uint activator)
        {
            var minutes = GetLocalInt(activator, IdleDespawnMinutesVariable);
            if (minutes <= 0)
                minutes = DefaultIdleDespawnMinutes;

            return TimeSpan.FromMinutes(minutes);
        }

        private static void MarkEncounterActivity(uint encounterCreature)
        {
            _lastActivityByCreature[encounterCreature] = DateTime.UtcNow;
        }

        private static void ScheduleIdleDespawnCheck(uint activator, uint encounterCreature, string encounterId)
        {
            var idleTimeout = GetIdleTimeout(activator);
            DelayCommand((float)idleTimeout.TotalSeconds, () => CheckIdleDespawn(activator, encounterCreature, encounterId));
        }

        private static void CheckIdleDespawn(uint activator, uint encounterCreature, string encounterId)
        {
            if (!GetIsObjectValid(encounterCreature) || GetIsDead(encounterCreature))
            {
                ClearActiveCreature(activator, encounterCreature, encounterId);
                return;
            }

            var now = DateTime.UtcNow;
            if (!_lastActivityByCreature.TryGetValue(encounterCreature, out var lastActivity))
                lastActivity = now;

            var idleTimeout = GetIdleTimeout(activator);
            if (IsIdleExpired(lastActivity, now, idleTimeout))
            {
                SendMessageToNearbyPlayers(encounterCreature, "The encounter fades after being left uncontested.");
                DestroyObject(encounterCreature);
                ClearActiveCreature(activator, encounterCreature, encounterId);
                return;
            }

            var remaining = idleTimeout - (now - lastActivity);
            DelayCommand((float)Math.Max(1.0, remaining.TotalSeconds), () => CheckIdleDespawn(activator, encounterCreature, encounterId));
        }

        private static void SendMessageToNearbyPlayers(uint encounterCreature, string message)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) != GetArea(encounterCreature)) continue;
                if (GetDistanceBetween(player, encounterCreature) > ParticipantCreditRange) continue;

                SendMessageToPC(player, message);
            }
        }
    }
}
