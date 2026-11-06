using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The combat points players earn against NPCs and the skill XP they turn into when the NPC dies. The logic lives in
    /// <see cref="ICombatPointService"/>; the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static class CombatPoint
    {
        private static ICombatPointService Instance => FacadeAccessor.Resolve<ICombatPointService>();

        /// <summary>
        /// Adds a combat point to a given NPC creature for a given player and skill type.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void OnHitCastSpell() =>
            Instance.OnHitCastSpell();

        /// <summary>Records one landed hit delivered by an item's on-hit event under the item's attack skill.</summary>
        public static void RecordItemHitPoint(uint player, uint target, uint item) =>
            Instance.RecordItemHitPoint(player, target, item);

        /// <summary>Records one landed logical weapon shot using its captured skill, through normal XP accounting.</summary>
        public static void RecordWeaponAttackPoint(uint actor, uint target, uint weapon, SkillType capturedSkill) =>
            Instance.RecordWeaponAttackPoint(actor, target, weapon, capturedSkill);

        /// <summary>Credits a landed native roll once and suppresses its later weapon/ammunition callback.</summary>
        public static void CaptureNativeWeaponAttackPoint(uint actor, uint target, uint weapon, SkillType capturedSkill, long rollId = 0, uint ammunition = OBJECT_INVALID, bool awardPoint = true) =>
            Instance.CaptureNativeWeaponAttackPoint(actor, target, weapon, capturedSkill, rollId, ammunition, awardPoint);

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearPendingWeaponAttackPoints() =>
            Instance.ClearPendingWeaponAttackPoints();

        /// <summary>
        /// When a creature dies, skill XP is given to all players who contributed during battle.
        /// Then, those combat points are cleared out.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnCreatureDeath() =>
            Instance.OnCreatureDeath();

        /// <summary>
        /// When a player leaves an area or the server, we need to remove all combat points
        /// that may be referenced to their character.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnPlayerExit() =>
            Instance.OnPlayerExit();

        /// <summary>
        /// Adds a combat point for a player to an NPC on a skill.
        /// </summary>
        /// <param name="player">The player receiving the point</param>
        /// <param name="creature">The creature to associate this point with.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddCombatPoint(uint player, uint creature, SkillType skill, int amount = 1) =>
            Instance.AddCombatPoint(player, creature, skill, amount);

        /// <summary>
        /// Returns true if the player has already earned any combat points against the given creature,
        /// i.e. they are actively participating in the fight with it.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="creature">The creature to check against.</param>
        public static bool HasCombatPoints(uint player, uint creature) =>
            Instance.HasCombatPoints(player, creature);

        /// <summary>
        /// Retrieves the level of the last enemy a player was involved in combat with.
        /// Returns -1 if it has been longer than two minutes since the level was updated
        /// or the value isn't there (some creatures are level 0).
        /// </summary>
        /// <param name="player">The player to retrieve from</param>
        /// <returns>The level of the last enemy a player was involved in combat with or zero if expired/unavailable.</returns>
        public static int GetRecentEnemyLevel(uint player) =>
            Instance.GetRecentEnemyLevel(player);

        /// <summary>
        /// Removes the details related to a recent enemy's details.
        /// This should be called after using the value returned by GetRecentEnemyLevel.
        /// </summary>
        /// <param name="player">The player to clear.</param>
        public static void ClearRecentEnemyLevel(uint player) =>
            Instance.ClearRecentEnemyLevel(player);

        /// <summary>
        /// Retrieves the count of creatures tagged by a given player.
        /// </summary>
        /// <param name="player">The player to check for tagged NPCs.</param>
        /// <returns>
        /// Number of creatures tagged by the player.
        /// 0 is none, -1 if the player is not initialized in the playerToCreatureTracker.
        /// </returns>
        public static int GetTaggedCreatureCount(uint player) =>
            Instance.GetTaggedCreatureCount(player);

        /// <summary>
        /// Adds a combat point for a player to all NPCs s/he is currently tagged on.
        /// </summary>
        /// <param name="player">The player to receiving the point.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddCombatPointToAllTagged(uint player, SkillType skill, int amount = 1) =>
            Instance.AddCombatPointToAllTagged(player, skill, amount);

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
        public static void AddSupportCombatPoint(uint player, IEnumerable<uint> supportedCreatures, SkillType skill, int amount = 1, ISet<uint> alreadyCredited = null) =>
            Instance.AddSupportCombatPoint(player, supportedCreatures, skill, amount, alreadyCredited);

        /// <summary>
        /// Adds a combat point for a player to every NPC they are tagged on and every NPC in combat
        /// with the single creature they supported.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="supportedCreature">The creature the player repaired, healed, or otherwise supported.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddSupportCombatPoint(uint player, uint supportedCreature, SkillType skill, int amount = 1) =>
            Instance.AddSupportCombatPoint(player, supportedCreature, skill, amount);

        /// <summary>
        /// Adds support combat points for bringing a creature back into a fight. A revived or newly
        /// summoned creature has no enmity of its own (death clears it), so the enemies fighting its
        /// nearby party members are credited instead.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="anchor">The revived creature, or the owner a summoned creature joins.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddReviveSupportCombatPoint(uint player, uint anchor, SkillType skill, int amount = 1) =>
            Instance.AddReviveSupportCombatPoint(player, anchor, skill, amount);

        /// <summary>
        /// Adds a player-to-npc reference to the cache.
        /// </summary>
        /// <param name="player">The player whose reference we're attaching.</param>
        /// <param name="creature">The creature we're referencing</param>
        public static void AddPlayerToNPCReferenceToCache(uint player, uint creature) =>
            Instance.AddPlayerToNPCReferenceToCache(player, creature);
    }
}
