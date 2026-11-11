using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.Service
{
    public static class CombatPoint
    {
        private const float ReviveSupportRangeMeters = 20f;

        /// <summary>
        /// Tracks the combat points earned by players during combat.
        /// </summary>
        private static readonly Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>> _creatureCombatPointTracker = new();

        /// <summary>
        /// Tracks the combat point lists associated with a player back to a creature.
        /// </summary>
        private static readonly Dictionary<uint, HashSet<uint>> _playerToCreatureTracker = new();
        private static readonly WeaponAttackPointLedger _weaponAttackPoints = new();

        /// <summary>
        /// Adds a combat point to a given NPC creature for a given player and skill type.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void OnHitCastSpell()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || !GetIsObjectValid(player)) return;

            var item = GetSpellCastItem();
            var target = GetSpellTargetObject();
            if (GetIsPC(target) || GetIsDM(target)) return;
            if (_weaponAttackPoints.Consume(player, target, item, DateTime.UtcNow)) return;

            RecordItemHitPoint(player, target, item);
        }

        /// <summary>Records one landed hit delivered by an item's on-hit event under the item's attack skill.</summary>
        public static void RecordItemHitPoint(uint player, uint target, uint item)
        {
            RecordWeaponAttackPoint(player, target, item, Skill.GetWeaponSkill(player, item));
        }

        /// <summary>Records one landed logical weapon shot using its captured skill, through normal XP accounting.</summary>
        public static void RecordWeaponAttackPoint(uint actor, uint target, uint weapon, SkillType capturedSkill)
        {
            if (!GetIsObjectValid(actor) || !GetIsPC(actor) || GetIsDM(actor) ||
                !GetIsObjectValid(target) || GetIsPC(target) || GetIsDM(target) ||
                capturedSkill == SkillType.Invalid) return;

            AddCombatPoint(actor, target, capturedSkill);

            // If player has a beast active, add a combat point for Beast Mastery.
            var associate = GetAssociate(AssociateType.Henchman, actor);
            if (BeastMastery.IsPlayerBeast(associate))
            {
                AddCombatPoint(actor, target, SkillType.BeastMastery);
            }
        }

        /// <summary>Credits a landed native roll once and suppresses its later weapon/ammunition callback.</summary>
        public static void CaptureNativeWeaponAttackPoint(uint actor, uint target, uint weapon,
            SkillType capturedSkill, long rollId = 0, uint ammunition = OBJECT_INVALID, bool awardPoint = true)
        {
            if (!GetIsObjectValid(actor) || !GetIsPC(actor) || GetIsDM(actor) ||
                !GetIsObjectValid(target) || GetIsPC(target) || GetIsDM(target) ||
                capturedSkill == SkillType.Invalid) return;
            if (!_weaponAttackPoints.Capture(actor, target, weapon, ammunition, rollId, DateTime.UtcNow)) return;
            // A queued ability owns the replaced roll's contribution under its captured ability skill.
            if (awardPoint) RecordWeaponAttackPoint(actor, target, weapon, capturedSkill);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearPendingWeaponAttackPoints() => _weaponAttackPoints.Clear(GetExitingObject());

        /// <summary>
        /// When a creature dies, skill XP is given to all players who contributed during battle.
        /// Then, those combat points are cleared out.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnCreatureDeath()
        {
            // Clears the combat point cache information for an NPC and all player associated.
            static void CleanUpCombatPoints()
            {
                var npc = OBJECT_SELF;

                if (_creatureCombatPointTracker.ContainsKey(npc))
                {
                    // Remove references from the player-to-npc cache.
                    foreach (var playerId in _creatureCombatPointTracker[npc].Keys)
                    {
                        RemovePlayerToNPCReferenceFromCache(playerId, npc);
                    }

                    _creatureCombatPointTracker.Remove(npc);
                }
            }

            // When a creature is killed, XP is calculated based on the combat points earned by each player.
            // This XP is distributed into skills which received the highest usage during the battle.
            static void DistributeSkillXP()
            {
                var npc = OBJECT_SELF;

                var combatPoints = _creatureCombatPointTracker.ContainsKey(npc) ? _creatureCombatPointTracker[npc] : null;
                if (combatPoints == null) return;

                var npcStats = Stat.GetNPCStats(npc);
                var npcLevel = npcStats.Level;

                foreach (var (player, cpList) in combatPoints)
                {
                    if (!GetIsObjectValid(player) ||
                        !GetIsPC(player) ||
                        GetIsDM(player) ||
                        GetIsDead(player) ||
                        GetCurrentHitPoints(player) <= 0 ||
                        GetDistanceBetween(player, npc) > 40.0f ||
                        GetArea(player) != GetArea(npc))
                        continue;

                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);

                    // Filter the skills list down to just those with combat points (CP)
                    var skillsWithCP = dbPlayer
                        .Skills
                        .Where(x => cpList.ContainsKey(x.Key))
                        .ToDictionary(x => x.Key, y => y.Value);

                    var areaBonus = GetLocalInt(GetArea(npc), "AREA_XP_BONUS_PERCENTAGE") * 0.01f;

                    foreach (CombatPointCategoryType cpCategory in Enum.GetValues(typeof(CombatPointCategoryType)))
                    {
                        var validSkills = skillsWithCP
                            .Where(x => Skill.GetSkillDetails(x.Key).CombatPointCategory == cpCategory)
                            .ToDictionary(x => x.Key, y => y.Value);

                        // Base amount of XP is determined by the player's highest-leveled skill rank in each XP category versus the creature's level.
                        if (cpCategory != CombatPointCategoryType.Exempt)
                        {
                            var validCPs = cpList
                                .Where(x => Skill.GetSkillDetails(x.Key).CombatPointCategory == cpCategory)
                                .ToList();
                            if (!validCPs.Any()) continue;
                            if (!validSkills.Any()) continue;

                            var highestRank = validSkills
                                .OrderByDescending(o => o.Value.Rank)
                                .Select(s => s.Value.Rank)
                                .First();

                            var xpDelta = npcLevel - highestRank;
                            var baseXP = Skill.GetDeltaXP(xpDelta);
                            var totalCatCP = (float)validCPs
                                .Sum(s => s.Value);

                            // Each skill used during combat receives a percentage of the baseXP amount depending on the number of combat points earned.
                            // The percentage is based on XP category, see CombatPointCategory

                            foreach (var (skillType, cp) in validCPs)
                            {
                                var adjXP = baseXP * (cp / totalCatCP);
                                adjXP += adjXP * areaBonus;
                                Skill.GiveSkillXP(player, skillType, (int)adjXP,
                                    xpMessageSource: skillType == SkillType.Mimicry ? npc : OBJECT_INVALID);
                            }
                        }
                        else
                        {
                            // Skills that are exempt from CP sharing; XP gain is calculated directly on a rank vs NPC level basis
                            // As long as the player is on the ground, we always try to give them Armor XP
                            if (!Space.IsPlayerInSpaceMode(player)) validSkills.Add(SkillType.Armor, dbPlayer.Skills[SkillType.Armor]);
                            if (!validSkills.Any()) continue;

                            foreach (var (skillType, ps) in validSkills)
                            {
                                float adjXP = Skill.GetDeltaXP(npcLevel - ps.Rank);
                                adjXP += adjXP * areaBonus;
                                Skill.GiveSkillXP(player, skillType, (int)adjXP);
                            }
                        }
                    }

                    EventsPlugin.PushEventData("NPC", ObjectToString(npc));
                    EventsPlugin.SignalEvent("SWLOR_COMBAT_POINT_DISTRIBUTED", player);
                }
            }

            DistributeSkillXP();
            CleanUpCombatPoints();
        }

        /// <summary>
        /// When a player leaves an area or the server, we need to remove all combat points
        /// that may be referenced to their character.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            ClearPlayerCombatPoints(player);
        }

        /// <summary>
        /// Removes all combat points for a player as well as all other cache references.
        /// </summary>
        /// <param name="player">The player whose cache data we're removing</param>
        private static void ClearPlayerCombatPoints(uint player)
        {
            if (!_playerToCreatureTracker.ContainsKey(player)) return;

            // Loop over all npcIds the player has linked to them.
            var npcIds = _playerToCreatureTracker[player];
            foreach (var npcId in npcIds)
            {
                if (!_creatureCombatPointTracker.ContainsKey(npcId)) continue;

                if (!_creatureCombatPointTracker[npcId].ContainsKey(player)) continue;
                _creatureCombatPointTracker[npcId].Remove(player);
            }

            _playerToCreatureTracker.Remove(player);
        }

        /// <summary>
        /// Adds a combat point for a player to an NPC on a skill.
        /// </summary>
        /// <param name="player">The player receiving the point</param>
        /// <param name="creature">The creature to associate this point with.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddCombatPoint(uint player, uint creature, SkillType skill, int amount = 1)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (GetIsPC(creature) || GetIsDM(creature)) return;

            var tracker = _creatureCombatPointTracker.ContainsKey(creature) ?
                _creatureCombatPointTracker[creature] :
                new Dictionary<uint, Dictionary<SkillType, int>>();

            // Add an entry for this player if it doesn't exist.
            if (!tracker.ContainsKey(player))
            {
                tracker[player] = new Dictionary<SkillType, int>();
                AddPlayerToNPCReferenceToCache(player, creature);
            }

            // Add an entry for this skill if it doesn't exist.
            if (!tracker[player].ContainsKey(skill))
            {
                tracker[player][skill] = 0;
            }

            // Increment points for this player and skill.
            tracker[player][skill] += amount;
            _creatureCombatPointTracker[creature] = tracker;

            // We track the level of the last creature to add a combat point for two minutes.
            // During this time period, various skills can continue to gain XP even after battle.
            var npcStats = Stat.GetNPCStats(creature);
            var level = npcStats.Level;
            UpdateLastCreatureLevel(player, level);
        }

        /// <summary>
        /// Returns true if the player has already earned any combat points against the given creature,
        /// i.e. they are actively participating in the fight with it.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="creature">The creature to check against.</param>
        public static bool HasCombatPoints(uint player, uint creature)
        {
            return _creatureCombatPointTracker.TryGetValue(creature, out var byPlayer) &&
                   byPlayer.ContainsKey(player);
        }

        /// <summary>
        /// Updates the level of the last creature associated with an added combat point.
        /// Also refreshes the expiration time by 2 minutes.
        /// </summary>
        /// <param name="player">The player to update.</param>
        /// <param name="level">The new level to assign.</param>
        private static void UpdateLastCreatureLevel(uint player, int level)
        {
            var expiration = DateTime.UtcNow.AddMinutes(2);
            SetLocalInt(player, "COMBAT_POINT_LAST_NPC_LEVEL", level);
            SetLocalString(player, "COMBAT_POINT_LAST_NPC_EXPIRATION", expiration.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Retrieves the level of the last enemy a player was involved in combat with.
        /// Returns -1 if it has been longer than two minutes since the level was updated
        /// or the value isn't there (some creatures are level 0).
        /// </summary>
        /// <param name="player">The player to retrieve from</param>
        /// <returns>The level of the last enemy a player was involved in combat with or zero if expired/unavailable.</returns>
        public static int GetRecentEnemyLevel(uint player)
        {
            var now = DateTime.UtcNow;
            var expirationString = GetLocalString(player, "COMBAT_POINT_LAST_NPC_EXPIRATION");
            if (string.IsNullOrWhiteSpace(expirationString))
                return -1;

            var expiration = DateTime.ParseExact(expirationString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (now >= expiration)
                return -1;

            return GetLocalInt(player, "COMBAT_POINT_LAST_NPC_LEVEL");
        }

        /// <summary>
        /// Removes the details related to a recent enemy's details.
        /// This should be called after using the value returned by GetRecentEnemyLevel.
        /// </summary>
        /// <param name="player">The player to clear.</param>
        public static void ClearRecentEnemyLevel(uint player)
        {
            DeleteLocalString(player, "COMBAT_POINT_LAST_NPC_EXPIRATION");
            DeleteLocalInt(player, "COMBAT_POINT_LAST_NPC_LEVEL");
        }

        /// <summary>
        /// Retrieves the count of creatures tagged by a given player.
        /// </summary>
        /// <param name="player">The player to check for tagged NPCs.</param>
        /// <returns>
        /// Number of creatures tagged by the player.
        /// 0 is none, -1 if the player is not initialized in the playerToCreatureTracker.
        /// </returns>
        public static int GetTaggedCreatureCount(uint player)
        {
            if (!_playerToCreatureTracker.ContainsKey(player))
                return -1;

            return _playerToCreatureTracker[player].Count;
        }

        /// <summary>
        /// Adds a combat point for a player to all NPCs s/he is currently tagged on.
        /// </summary>
        /// <param name="player">The player to receiving the point.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddCombatPointToAllTagged(uint player, SkillType skill, int amount = 1)
        {
            if (!_playerToCreatureTracker.ContainsKey(player))
                return;

            foreach (var creature in _playerToCreatureTracker[player])
            {
                AddCombatPoint(player, creature, skill, amount);
            }
        }

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
        public static void AddSupportCombatPoint(
            uint player,
            IEnumerable<uint> supportedCreatures,
            SkillType skill,
            int amount = 1,
            ISet<uint> alreadyCredited = null)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var creatures = _playerToCreatureTracker.TryGetValue(player, out var tagged)
                ? new HashSet<uint>(tagged)
                : new HashSet<uint>();

            foreach (var supported in supportedCreatures)
            {
                foreach (var enemy in Enmity.GetEnmityTowardsAllEnemies(supported).Keys)
                {
                    if (Enmity.HasNonProximityEnmity(supported, enemy))
                        creatures.Add(enemy);
                }
            }

            foreach (var creature in creatures)
            {
                if (alreadyCredited != null && !alreadyCredited.Add(creature))
                    continue;

                AddCombatPoint(player, creature, skill, amount);
            }
        }

        /// <summary>
        /// Adds a combat point for a player to every NPC they are tagged on and every NPC in combat
        /// with the single creature they supported.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="supportedCreature">The creature the player repaired, healed, or otherwise supported.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddSupportCombatPoint(uint player, uint supportedCreature, SkillType skill, int amount = 1)
        {
            AddSupportCombatPoint(player, new[] { supportedCreature }, skill, amount);
        }

        /// <summary>
        /// Adds support combat points for bringing a creature back into a fight. A revived or newly
        /// summoned creature has no enmity of its own (death clears it), so the enemies fighting its
        /// nearby party members are credited instead.
        /// </summary>
        /// <param name="player">The player receiving the point.</param>
        /// <param name="anchor">The revived creature, or the owner a summoned creature joins.</param>
        /// <param name="skill">The skill to associate with the point.</param>
        /// <param name="amount">The number of points to add.</param>
        public static void AddReviveSupportCombatPoint(uint player, uint anchor, SkillType skill, int amount = 1)
        {
            AddSupportCombatPoint(player, Party.GetAllPartyMembersWithinRange(anchor, ReviveSupportRangeMeters), skill, amount);
        }

        /// <summary>
        /// Adds a player-to-npc reference to the cache.
        /// </summary>
        /// <param name="player">The player whose reference we're attaching.</param>
        /// <param name="creature">The creature we're referencing</param>
        public static void AddPlayerToNPCReferenceToCache(uint player, uint creature)
        {
            if (!_playerToCreatureTracker.ContainsKey(player))
            {
                _playerToCreatureTracker[player] = new HashSet<uint>();
            }

            if (!_playerToCreatureTracker[player].Contains(creature))
            {
                _playerToCreatureTracker[player].Add(creature);
            }
        }

        /// <summary>
        /// Removes a player-to-npc reference from the cache.
        /// </summary>
        /// <param name="player">The player whose reference we're removing</param>
        /// <param name="npc">The creature we're referencing</param>
        private static void RemovePlayerToNPCReferenceFromCache(uint player, uint npc)
        {
            if (!_playerToCreatureTracker.ContainsKey(player)) return;

            if (_playerToCreatureTracker[player].Contains(npc))
            {
                _playerToCreatureTracker[player].Remove(npc);
            }
        }
    }
}
