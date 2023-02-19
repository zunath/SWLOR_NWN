using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    public static class CombatPoint
    {
        /// <summary>
        /// Tracks the combat points earned by players during combat.
        /// </summary>
        private static readonly Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>> _creatureCombatPointTracker = new();

        /// <summary>
        /// Tracks the combat point lists associated with a player back to a creature.
        /// </summary>
        private static readonly Dictionary<uint, HashSet<uint>> _playerToCreatureTracker = new();

        /// <summary>
        /// Adds a combat point to a given NPC creature for a given player and skill type.
        /// </summary>
        [NWNEventHandler("item_on_hit")]
        public static void OnHitCastSpell()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || !GetIsObjectValid(player)) return;

            var item = GetSpellCastItem();
            var baseItemType = GetBaseItemType(item);
            var target = GetSpellTargetObject();
            if (GetIsPC(target) || GetIsDM(target)) return;

            var skill = Skill.GetSkillTypeByBaseItem(baseItemType);
            if (skill == SkillType.Invalid) return;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var levelDelta = dbPlayer.Skills[SkillType.Force].Rank - dbPlayer.Skills[skill].Rank;

            AddCombatPoint(player, target, skill);

            // Lightsabers and Saberstaffs automatically grant combat points toward Force if player has the setting enabled.
            // Additionally, a force combat point is only added if the force skill is not 5 more levels above the one-handed or two-handed skill being used.
            if ((Item.LightsaberBaseItemTypes.Contains(baseItemType) ||
                Item.SaberstaffBaseItemTypes.Contains(baseItemType)) &&
                dbPlayer.CharacterType == CharacterType.ForceSensitive &&
                dbPlayer.Settings.IsLightsaberForceShareEnabled &&
                levelDelta <= 5)
            {
                AddCombatPoint(player, target, SkillType.Force);
            }

            // If player has a beast active, add a combat point for Beast Mastery.
            var associate = GetAssociate(AssociateType.Henchman, player);
            if (BeastMastery.IsPlayerBeast(associate))
            {
                AddCombatPoint(player, target, SkillType.BeastMastery);
            }
        }

        /// <summary>
        /// When a creature dies, skill XP is given to all players who contributed during battle.
        /// Then, those combat points are cleared out.
        /// </summary>
        [NWNEventHandler("crea_death_aft")]
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
                                Skill.GiveSkillXP(player, skillType, (int)adjXP);
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
        [NWNEventHandler("mod_exit")]
        [NWNEventHandler("area_exit")]
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
