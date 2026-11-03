using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// The static API of the quest encounters. The logic lives in <see cref="IQuestEncounterService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change. This is the extra facade of the <c>Quest</c> service (<c>ConvertedServices.json</c>).
    /// </summary>
    public static class QuestEncounter
    {
        // Compile-time constants: callers get the values inlined, as before.
        public const string EncounterIdVariable = Domain.Quests.QuestEncounterService.EncounterIdVariable;
        public const string CreatureResrefVariable = Domain.Quests.QuestEncounterService.CreatureResrefVariable;
        public const string SpawnWaypointVariable = Domain.Quests.QuestEncounterService.SpawnWaypointVariable;
        public const string CooldownMinutesVariable = Domain.Quests.QuestEncounterService.CooldownMinutesVariable;
        public const string IdleDespawnMinutesVariable = Domain.Quests.QuestEncounterService.IdleDespawnMinutesVariable;
        public const string ActiveCreatureVariable = Domain.Quests.QuestEncounterService.ActiveCreatureVariable;
        public const string ActivatorVariable = Domain.Quests.QuestEncounterService.ActivatorVariable;
        public const string EncounterCreatureFlagVariable = Domain.Quests.QuestEncounterService.EncounterCreatureFlagVariable;

        private static IQuestEncounterService Instance => FacadeAccessor.Resolve<IQuestEncounterService>();

        /// <summary>When a player enters the module, refresh which encounter activators they can see.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            Instance.OnModuleEnter();
        }

        /// <summary>When a player enters an area, refresh which encounter activators they can see.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            Instance.OnAreaEnter();
        }

        /// <summary>When a player uses an encounter activator, spawn its creature.</summary>
        [NWNEventHandler(ScriptName.OnQuestEncounter)]
        public static void UseActivator()
        {
            Instance.UseActivator();
        }

        /// <summary>Marks the encounter creature as active when it attacks or is damaged.</summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackAfter)]
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void TrackEncounterActivity()
        {
            Instance.TrackEncounterActivity();
        }

        /// <summary>Clears the encounter when its creature is destroyed.</summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void ObjectDestroyed()
        {
            Instance.ObjectDestroyed();
        }

        /// <summary>True when the creature was spawned by an encounter activator.</summary>
        public static bool IsQuestEncounterCreature(uint encounterCreature)
        {
            return Instance.IsQuestEncounterCreature(encounterCreature);
        }

        /// <summary>Credits the kill to every player who took part in the encounter.</summary>
        public static void ProgressKillCredit(uint encounterCreature, NPCGroupType npcGroupType, IReadOnlyCollection<string> possibleQuests)
        {
            Instance.ProgressKillCredit(encounterCreature, npcGroupType, possibleQuests);
        }

        /// <summary>True when the player is on exactly this quest state and has not completed the quest.</summary>
        public static bool IsPlayerOnQuestState(Player dbPlayer, string questId, int questState)
        {
            return Instance.IsPlayerOnQuestState(dbPlayer, questId, questState);
        }

        /// <summary>The time left before the player can call the encounter again, or zero.</summary>
        public static TimeSpan GetCooldownRemaining(Player dbPlayer, string encounterId, DateTime now)
        {
            return Instance.GetCooldownRemaining(dbPlayer, encounterId, now);
        }

        /// <summary>True when the creature has been left alone for the whole idle timeout.</summary>
        public static bool IsIdleExpired(DateTime lastActivity, DateTime now, TimeSpan idleTimeout)
        {
            return Instance.IsIdleExpired(lastActivity, now, idleTimeout);
        }

        /// <summary>Forgets the creature and shows its activator again.</summary>
        public static void ClearEncounterForCreature(uint encounterCreature)
        {
            Instance.ClearEncounterForCreature(encounterCreature);
        }

        /// <summary>Shows or hides each encounter activator in the player's area according to their quest state.</summary>
        public static void RefreshVisibilityForPlayer(uint player)
        {
            Instance.RefreshVisibilityForPlayer(player);
        }

        /// <summary>Refreshes the activator visibility of every player in the area.</summary>
        public static void RefreshVisibilityForArea(uint area)
        {
            Instance.RefreshVisibilityForArea(area);
        }
    }
}
