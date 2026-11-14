using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>The combat points players earn against NPCs and the skill XP they turn into when the NPC dies.</summary>
    public interface ICombatPointService
    {
        /// <summary>
        /// Adds a combat point to a given NPC creature for a given player and skill type.
        /// </summary>
        void OnHitCastSpell();

        /// <summary>Records one landed hit delivered by an item's on-hit event under the item's attack skill.</summary>
        void RecordItemHitPoint(uint player, uint target, uint item);

        /// <summary>Records one landed logical weapon shot using its captured skill, through normal XP accounting.</summary>
        void RecordWeaponAttackPoint(uint actor, uint target, uint weapon, SkillType capturedSkill);

        /// <summary>Credits a landed native roll once and suppresses its later weapon/ammunition callback.</summary>
        void CaptureNativeWeaponAttackPoint(uint actor, uint target, uint weapon, SkillType capturedSkill, long rollId = 0, uint ammunition = OBJECT_INVALID, bool awardPoint = true);

        void ClearPendingWeaponAttackPoints();

        /// <summary>
        /// When a creature dies, skill XP is given to all players who contributed during battle.
        /// Then, those combat points are cleared out.
        /// </summary>
        void OnCreatureDeath();

        /// <summary>
        /// When a player leaves an area or the server, we need to remove all combat points
        /// that may be referenced to their character.
        /// </summary>
        void OnPlayerExit();

        /// <summary>
        /// Adds a combat point for a player to an NPC on a skill.
        /// </summary>
        /// <param name="player">The player receiving the point</param>
        /// <param name="creature">The creature to associate this point with.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        void AddCombatPoint(uint player, uint creature, SkillType skill, int amount = 1);

        /// <summary>
        /// Returns true if the player has already earned any combat points against the given creature,
        /// i.e. they are actively participating in the fight with it.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="creature">The creature to check against.</param>
        bool HasCombatPoints(uint player, uint creature);

        /// <summary>
        /// Retrieves the level of the last enemy a player was involved in combat with.
        /// Returns -1 if it has been longer than two minutes since the level was updated
        /// or the value isn't there (some creatures are level 0).
        /// </summary>
        /// <param name="player">The player to retrieve from</param>
        /// <returns>The level of the last enemy a player was involved in combat with or zero if expired/unavailable.</returns>
        int GetRecentEnemyLevel(uint player);

        /// <summary>
        /// Removes the details related to a recent enemy's details.
        /// This should be called after using the value returned by GetRecentEnemyLevel.
        /// </summary>
        /// <param name="player">The player to clear.</param>
        void ClearRecentEnemyLevel(uint player);

        /// <summary>
        /// Retrieves the count of creatures tagged by a given player.
        /// </summary>
        /// <param name="player">The player to check for tagged NPCs.</param>
        /// <returns>
        /// Number of creatures tagged by the player.
        /// 0 is none, -1 if the player is not initialized in the playerToCreatureTracker.
        /// </returns>
        int GetTaggedCreatureCount(uint player);

        /// <summary>
        /// Adds a combat point for a player to all NPCs s/he is currently tagged on.
        /// </summary>
        /// <param name="player">The player to receiving the point.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        void AddCombatPointToAllTagged(uint player, SkillType skill, int amount = 1);

        /// <summary>
        /// Adds a combat point for a player to every NPC they are tagged on and every NPC in combat
        /// with the creatures they supported, so a supporter who never attacked still earns XP when
        /// those enemies die. Aggro-proximity enmity alone does not count as combat.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="supportedCreatures">The creatures the player repaired, healed, or otherwise supported.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        /// <param name="alreadyCredited">
        /// Optional per-cast record for support that applies over time. Creatures already in it are
        /// skipped and newly credited creatures are added, so each enemy is credited once per cast.
        /// </param>
        void AddSupportCombatPoint(uint player, IEnumerable<uint> supportedCreatures, SkillType skill, int amount = 1, ISet<uint> alreadyCredited = null);

        /// <summary>
        /// Adds a combat point for a player to every NPC they are tagged on and every NPC in combat
        /// with the single creature they supported.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="supportedCreature">The creature the player repaired, healed, or otherwise supported.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        void AddSupportCombatPoint(uint player, uint supportedCreature, SkillType skill, int amount = 1);

        /// <summary>
        /// Adds support combat points for bringing a creature back into a fight. A revived or newly
        /// summoned creature has no enmity of its own (death clears it), so the enemies fighting its
        /// nearby party members are credited instead.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="anchor">The revived creature, or the owner a summoned creature joins.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        void AddReviveSupportCombatPoint(uint player, uint anchor, SkillType skill, int amount = 1);

        /// <summary>
        /// Adds a player-to-npc reference to the cache.
        /// </summary>
        /// <param name="player">The player whose reference we're attaching.</param>
        /// <param name="creature">The creature we're referencing</param>
        void AddPlayerToNPCReferenceToCache(uint player, uint creature);
    }
}
