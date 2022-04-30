using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        /// <summary>
        /// This is the maximum number of skill points a single character can have at any time.
        /// </summary>
        public const int SkillCap = 300;

        /// <summary>
        /// Gives XP towards a specific skill to a player.
        /// </summary>
        /// <param name="player">The player to give XP to.</param>
        /// <param name="skill">The type of skill to give XP towards.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="ignoreBonuses">If true, bonuses from food and other sources will NOT be applied.</param>
        public static void GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false)
        {
            if (skill == SkillType.Invalid || xp <= 0 || !GetIsPC(player) || GetIsDM(player)) return;

            var modifiedSkills = new List<SkillType>();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var details = GetSkillDetails(skill);
            var pcSkill = dbPlayer.Skills[skill];
            var requiredXP = GetRequiredXP(pcSkill.Rank);
            var receivedRankUp = false;

            if (!ignoreBonuses)
            {
                // Bonus for positive Social modifier.
                var social = GetAbilityModifier(AbilityType.Social, player);
                if (social > 0)
                    xp += (int)(xp * social * 0.05f);

                // Food bonus
                var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
                if (foodEffect != null)
                {
                    xp += (int)(xp * (foodEffect.XPBonusPercent * 0.01f));
                }
            }

            var debtRemoved = 0;
            if (dbPlayer.XPDebt > 0)
            {
                if (xp >= dbPlayer.XPDebt)
                {
                    debtRemoved = dbPlayer.XPDebt;
                    xp -= dbPlayer.XPDebt;
                }
                else
                {
                    debtRemoved = xp;
                    xp = 0;
                }
            }

            if (debtRemoved > 0)
            {
                dbPlayer.XPDebt -= debtRemoved;
                SendMessageToPC(player, $"{debtRemoved} XP was removed from your debt. (Remaining: {dbPlayer.XPDebt})");
            }

            if (xp <= 0)
                return;

            var totalRanks = dbPlayer.Skills
                .Where(x =>
                {
                    var detail = GetSkillDetails(x.Key);
                    return detail.ContributesToSkillCap;
                })
                .Sum(x => x.Value.Rank);
            var skillsPossibleToDecay = dbPlayer.Skills
                .Where(x =>
                {
                    var detail = GetSkillDetails(x.Key);

                    return !x.Value.IsLocked &&
                           detail.ContributesToSkillCap &&
                           x.Key != skill &&
                           x.Value.Rank > 0;
                }).ToList();

            // If player is at the skill cap and no skills are available for decay, exit early.
            if (skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
                return;

            SendMessageToPC(player, $"You earned {details.Name} skill experience. ({xp})");
            pcSkill.XP += xp;
            // Skill is at cap. No additional XP can be acquired.
            if (pcSkill.Rank >= details.MaxRank)
            {
                pcSkill.XP = 0;
            }

            while (pcSkill.XP >= requiredXP)
            {
                if (skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
                    break;

                receivedRankUp = true;
                pcSkill.XP -= requiredXP;

                if (dbPlayer.TotalSPAcquired < SkillCap && details.ContributesToSkillCap)
                {
                    dbPlayer.UnallocatedSP++;
                    dbPlayer.TotalSPAcquired++;
                }

                pcSkill.Rank++;
                FloatingTextStringOnCreature($"Your {details.Name} skill level increased to rank {pcSkill.Rank}!", player, false);

                requiredXP = GetRequiredXP(pcSkill.Rank);
                if (pcSkill.Rank >= details.MaxRank)
                {
                    pcSkill.XP = 0;
                }

                dbPlayer.Skills[skill] = pcSkill;

                ApplyAbilityPoint(player, pcSkill.Rank, dbPlayer);

                // If player is at the cap, pick a random skill out of the available decayable skills
                // reduce its level by 1 and set XP to zero.
                if (totalRanks >= SkillCap)
                {
                    var index = Random.Next(skillsPossibleToDecay.Count);
                    var decaySkill = skillsPossibleToDecay[index];
                    dbPlayer.Skills[decaySkill.Key].XP = 0;
                    dbPlayer.Skills[decaySkill.Key].Rank--;

                    if(!modifiedSkills.Contains(decaySkill.Key))
                        modifiedSkills.Add(decaySkill.Key);

                    if (dbPlayer.Skills[decaySkill.Key].Rank <= 0)
                        skillsPossibleToDecay.Remove(decaySkill);
                }

            }

            DB.Set(dbPlayer);

            modifiedSkills.Add(skill);
            Gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(modifiedSkills));

            // Send out an event signifying that a player has received a skill rank increase.
            if(receivedRankUp)
            {
                EventsPlugin.SignalEvent("SWLOR_GAIN_SKILL_POINT", player);
            }
        }

        /// <summary>
        /// Gives the player an ability point which can be distributed to the attribute of their choice
        /// from the character menu. Earned at 0.1 points per skill rank.  
        /// </summary>
        /// <param name="player">The player to receive the AP.</param>
        /// <param name="rank">The rank attained.</param>
        /// <param name="dbPlayer">The database entity.</param>
        private static void ApplyAbilityPoint(uint player, int rank, Player dbPlayer)
        {
            // Total AP have been earned (300SP = 30AP)
            if (dbPlayer.TotalAPAcquired >= SkillCap) return;

            void Apply(int expectedRank, int apLevelMax)
            {
                if (!dbPlayer.AbilityPointsByLevel.ContainsKey(expectedRank))
                    dbPlayer.AbilityPointsByLevel[expectedRank] = 0;

                if (rank == expectedRank &&
                    dbPlayer.AbilityPointsByLevel[expectedRank] < apLevelMax)
                {
                    dbPlayer.TotalAPAcquired++;
                    dbPlayer.AbilityPointsByLevel[expectedRank]++;

                    if (dbPlayer.TotalAPAcquired % 10 == 0)
                    {
                        dbPlayer.UnallocatedAP++;

                        SendMessageToPC(player, ColorToken.Green("You acquired 1 ability point!"));
                    }
                }
            }

            for (var level = 1; level <= 50; level++)
            {
                Apply(level, 8);
            }
        }

        /// <summary>
        /// Handles applying skill XP decay when a player has reached the skill cap.
        /// If decay cannot be applied, false will be returned.
        /// If decay was unnecessary or succeeded, true will be returned.
        /// </summary>
        /// <param name="dbPlayer">The player entity to apply skill decay to</param>
        /// <param name="skill">The skill which is receiving XP. This skill will be excluded from decay.</param>
        /// <param name="xp">The amount of XP being applied.</param>
        /// <param name="modifiedSkillTypes">A list of skill types which were modified as part of the decay process.</param>
        /// <returns>true if successful or unnecessary, false otherwise</returns>
        private static bool ApplyDecay(Player dbPlayer, SkillType skill, int xp, out List<SkillType> modifiedSkillTypes)
        {
            modifiedSkillTypes = new List<SkillType>();
            
            if (dbPlayer.TotalSPAcquired < SkillCap) 
                return true;

            var skillsPossibleToDecay = dbPlayer.Skills
                .Where(x =>
                {
                    var detail = GetSkillDetails(x.Key);

                    return !x.Value.IsLocked &&
                           detail.ContributesToSkillCap &&
                           x.Key != skill &&
                           (x.Value.XP > 0 || x.Value.Rank > 0);
                }).ToList();

            // If player is somehow not at the level cap (probably due to a prior decay), return true.
            if (skillsPossibleToDecay.Sum(x => x.Value.Rank) < SkillCap)
                return true;

            // If no skills can be decayed, return false.
            if (!skillsPossibleToDecay.Any()) 
                return false;

            // Get the total XP acquired, then add up any remaining XP for a partial level
            var totalAvailableXPToDecay = skillsPossibleToDecay.Sum(x =>
            {
                var totalXP = GetTotalRequiredXP(x.Value.Rank);
                totalXP += x.Value.XP;

                return totalXP;
            });

            // There's not enough XP to decay for this gain. Exit early.
            if (totalAvailableXPToDecay < xp) 
                return false;

            while (xp > 0)
            {
                var index = Random.Next(skillsPossibleToDecay.Count);
                var decaySkill = skillsPossibleToDecay[index];
                var totalDecayXP = GetTotalRequiredXP(decaySkill.Value.Rank) - GetRequiredXP(decaySkill.Value.Rank) + decaySkill.Value.XP;

                if (totalDecayXP >= xp)
                {
                    totalDecayXP -= xp;
                    xp = 0;
                }
                else if (totalDecayXP < xp)
                {
                    totalDecayXP = 0;
                    xp -= totalDecayXP;
                }

                // If skill drops to 0 total XP, remove it from the possible list of skills
                if (totalDecayXP <= 0)
                {
                    skillsPossibleToDecay.Remove(decaySkill);
                    decaySkill.Value.XP = 0;
                    decaySkill.Value.Rank = 0;
                }
                // Otherwise calculate what rank and XP value the skill should now be.
                else
                {
                    var (newDecaySkillRank, remainderXP) = GetLevelByTotalXP(totalDecayXP);

                    decaySkill.Value.Rank = newDecaySkillRank;
                    decaySkill.Value.XP = remainderXP;
                }

                modifiedSkillTypes.Add(decaySkill.Key);
                dbPlayer.Skills[decaySkill.Key].Rank = decaySkill.Value.Rank;
                dbPlayer.Skills[decaySkill.Key].XP = decaySkill.Value.XP;
            }

            // Save all changes made.
            DB.Set(dbPlayer);
            return true;
        }

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void AddMissingSkills()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            foreach (var skill in GetAllSkills())
            {
                if (!dbPlayer.Skills.ContainsKey(skill.Key))
                {
                    dbPlayer.Skills[skill.Key] = new PlayerSkill();
                }
            }

            DB.Set(dbPlayer);
        }
    }
}
