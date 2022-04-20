﻿using System.Linq;
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
        public static void GiveSkillXP(uint player, SkillType skill, int xp)
        {
            if (skill == SkillType.Invalid || xp <= 0 || !GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var details = GetSkillDetails(skill);
            var pcSkill = dbPlayer.Skills[skill];
            var requiredXP = GetRequiredXP(pcSkill.Rank);
            var receivedRankUp = false;

            // Bonus for positive Social modifier.
            var social = GetAbilityModifier(AbilityType.Social, player);
            if (social > 0) 
                xp += (int) (xp * social * 0.05f);

            // Food bonus
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
            if (foodEffect != null)
            {
                xp += (int)(xp * (foodEffect.XPBonusPercent * 0.01f));
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

            if (!ApplyDecay(dbPlayer, player, skill, xp))
            {
                return;
            }


            if (debtRemoved > 0)
            {
                dbPlayer.XPDebt = dbPlayer.XPDebt - debtRemoved;
                SendMessageToPC(player, $"{debtRemoved} XP was removed from your debt. (Remaining: {dbPlayer.XPDebt})");
            }

            if (xp > 0)
            {
                SendMessageToPC(player, $"You earned {details.Name} skill experience. ({xp})");
            }
            else if (debtRemoved == 0)
            {
                return;
            }
            
            pcSkill.XP += xp;
            // Skill is at cap and player would level up.
            // Reduce XP to required amount minus 1 XP.
            if (pcSkill.Rank >= details.MaxRank && pcSkill.XP > requiredXP)
            {
                pcSkill.XP = requiredXP - 1;
            }

            while (pcSkill.XP >= requiredXP)
            {
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
                if (pcSkill.Rank >= details.MaxRank && pcSkill.XP >= requiredXP)
                {
                    pcSkill.XP = requiredXP - 1;
                }

                dbPlayer.Skills[skill] = pcSkill;

                ApplyAbilityPoint(player, pcSkill.Rank, dbPlayer);
            }

            DB.Set(dbPlayer);

            Gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(skill));

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
        /// <param name="player">The player object.</param>
        /// <param name="skill">The skill which is receiving XP. This skill will be excluded from decay.</param>
        /// <param name="xp">The amount of XP being applied.</param>
        /// <returns>true if successful or unnecessary, false otherwise</returns>
        private static bool ApplyDecay(Player dbPlayer, uint player, SkillType skill, int xp)
        {
            if (dbPlayer.TotalSPAcquired < SkillCap) return true;

            var playerId = GetObjectUUID(player);
            var skillsPossibleToDecay = dbPlayer.Skills
                .Where(x =>
                {
                    var detail = GetSkillDetails(x.Key);

                    return !x.Value.IsLocked &&
                           detail.ContributesToSkillCap &&
                           x.Key != skill &&
                           (x.Value.XP > 0 || x.Value.Rank > 0);
                }).ToList();

            // If no skills can be decayed, return false.
            if (!skillsPossibleToDecay.Any()) return false;

            // Get the total XP acquired, then add up any remaining XP for a partial level
            int totalAvailableXPToDecay = skillsPossibleToDecay.Sum(x =>
            {
                var totalXP = GetTotalXP(x.Value.Rank);
                xp += x.Value.XP;

                return totalXP;
            });

            // There's not enough XP to decay for this gain. Exit early.
            if (totalAvailableXPToDecay < xp) return false;

            while (xp > 0)
            {
                var index = Random.Next(skillsPossibleToDecay.Count());
                var decaySkill = skillsPossibleToDecay[index];
                int totalDecayXP = GetTotalXP(decaySkill.Value.Rank) + decaySkill.Value.XP;

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
                    // Get the XP amounts required per level, in ascending order, so we can see how many levels we're now meant to have. 
                    var reqs = _skillTotalXP.Where(x => x.Key <= decaySkill.Value.Rank).OrderBy(o => o.Key); 

                    // The first entry in the database is for rank 0, and if passed, will raise us to 1.  So start our count at 0.
                    int newDecaySkillRank = 0;
                    foreach (var req in reqs)
                    {
                        if (totalDecayXP >= req.Value)
                        {
                            totalDecayXP -= req.Value;
                            newDecaySkillRank++;
                        }
                        else if (totalDecayXP < req.Value)
                        {
                            break;
                        }
                    }

                    decaySkill.Value.Rank = newDecaySkillRank;
                    decaySkill.Value.XP = totalDecayXP;
                }

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
