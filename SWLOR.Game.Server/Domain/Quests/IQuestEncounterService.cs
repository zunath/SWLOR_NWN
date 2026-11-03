using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Domain.Quests
{
    /// <summary>
    /// Quest encounters: placeables that spawn a creature for a player who is on the right quest state, the cooldown
    /// and idle despawn of that creature, and the kill credit its participants receive. The static facade
    /// <c>Service.QuestService.QuestEncounter</c> forwards to the container's instance of this interface and keeps the
    /// event handlers.
    /// </summary>
    public interface IQuestEncounterService
    {
        /// <summary>When a player enters the module, refresh which encounter activators they can see.</summary>
        void OnModuleEnter();

        /// <summary>When a player enters an area, refresh which encounter activators they can see.</summary>
        void OnAreaEnter();

        /// <summary>When a player uses an encounter activator, spawn its creature.</summary>
        void UseActivator();

        /// <summary>Marks the encounter creature as active when it attacks or is damaged.</summary>
        void TrackEncounterActivity();

        /// <summary>Clears the encounter when its creature is destroyed.</summary>
        void ObjectDestroyed();

        /// <summary>True when the creature was spawned by an encounter activator.</summary>
        bool IsQuestEncounterCreature(uint encounterCreature);

        /// <summary>Credits the kill to every player who took part in the encounter.</summary>
        void ProgressKillCredit(uint encounterCreature, NPCGroupType npcGroupType, IReadOnlyCollection<string> possibleQuests);

        /// <summary>True when the player is on exactly this quest state and has not completed the quest.</summary>
        bool IsPlayerOnQuestState(Player dbPlayer, string questId, int questState);

        /// <summary>The time left before the player can call the encounter again, or zero.</summary>
        TimeSpan GetCooldownRemaining(Player dbPlayer, string encounterId, DateTime now);

        /// <summary>True when the creature has been left alone for the whole idle timeout.</summary>
        bool IsIdleExpired(DateTime lastActivity, DateTime now, TimeSpan idleTimeout);

        /// <summary>Forgets the creature and shows its activator again.</summary>
        void ClearEncounterForCreature(uint encounterCreature);

        /// <summary>Shows or hides each encounter activator in the player's area according to their quest state.</summary>
        void RefreshVisibilityForPlayer(uint player);

        /// <summary>Refreshes the activator visibility of every player in the area.</summary>
        void RefreshVisibilityForArea(uint area);
    }
}
