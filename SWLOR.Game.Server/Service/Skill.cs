using System.Collections.Generic;
using System.Linq;
using Discord;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        /// <summary>
        /// This is the maximum number of skill points a single character can have at any time.
        /// </summary>
        public const int SkillCap = 350;

        /// <summary>
        /// This is the maximum number of AP a single character can earn in total. This must be evenly divisible into SkillCap.
        /// </summary>
        public static int APCap { get; } = SkillCap / 10;

        /// <summary>
        /// Gives XP towards a specific skill to a player.
        /// </summary>
        /// <param name="player">The player to give XP to.</param>
        /// <param name="skill">The type of skill to give XP towards.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="ignoreBonuses">If true, bonuses from food and other sources will NOT be applied.</param>
        /// <param name="applyHenchmanPenalty">If true, a penalty will apply if the player has a henchman active (droid, pet, etc.)</param>
        public static void GiveSkillXP(
            uint player, 
            SkillType skill, 
            int xp, 
            bool ignoreBonuses = false,
            bool applyHenchmanPenalty = true)
        {
            if (skill == SkillType.Invalid || xp <= 0 || !GetIsPC(player) || GetIsDM(player)) return;

            var modifiedSkills = new List<SkillType>();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var details = GetSkillDetails(skill);
            var pcSkill = dbPlayer.Skills[skill];
            var requiredXP = GetRequiredXP(pcSkill.Rank);
            var receivedRankUp = false;
            var bonusPercentage = 0f;
            var decayedSkills = new List<SkillType>();

            if (!ignoreBonuses)
            {
                // Bonus for positive Social modifier.
                var social = GetAbilityScore(player, AbilityType.Social);
                if (social > 0)
                    bonusPercentage += social * 0.025f;

                // Food bonus
                var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
                if (foodEffect != null)
                {
                    bonusPercentage += foodEffect.XPBonusPercent * 0.01f;
                }

                // DM bonus
                bonusPercentage += dbPlayer.DMXPBonus * 0.01f;

                // Dedication bonus
                if (StatusEffect.HasStatusEffect(player, StatusEffectType.Dedication))
                {
                    var source = StatusEffect.GetEffectData<uint>(player, StatusEffectType.Dedication);

                    if (GetIsObjectValid(source))
                    {
                        var effectiveLevel = Perk.GetPerkLevel(source, PerkType.Dedication);
                        social = GetAbilityScore(source, AbilityType.Social);
                        bonusPercentage += (10 + effectiveLevel * social) * 0.01f;
                    }
                }

                // Apply bonuses
                xp += (int)(xp * bonusPercentage);
            }

            // 30% penalty applied if a Henchman is active.
            if (applyHenchmanPenalty)
            {
                const float HenchmanPenalty = 0.3f;

                xp -= (int)(xp * HenchmanPenalty);
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
            {
                DB.Set(dbPlayer);
                return;
            }
            
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
                }).Select(s => s.Key).ToList();

            // If player is at the skill cap and no skills are available for decay, exit early.
            if (details.ContributesToSkillCap && skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
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
                if (details.ContributesToSkillCap && skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
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

                if (details.ContributesToSkillCap)
                {
                    ApplyAbilityPoint(player, dbPlayer);
                }
                
                totalRanks = dbPlayer.Skills
                    .Where(x =>
                    {
                        var detail = GetSkillDetails(x.Key);
                        return detail.ContributesToSkillCap;
                    })
                    .Sum(x => x.Value.Rank);

                // If player is at the cap, pick a random skill out of the available decayable skills
                // reduce its level by 1 and set XP to zero.
                if (details.ContributesToSkillCap && totalRanks >= SkillCap)
                {
                    // Edge case: Part of the number of levels granted cannot be given because
                    // there are no decayable skills to reduce. All excess XP is lost and we
                    // no longer need to proceed with increasing the skill rank
                    if (skillsPossibleToDecay.Count <= 0)
                    {
                        dbPlayer.Skills[skill].XP = 0;
                        break;
                    }

                    var index = Random.Next(skillsPossibleToDecay.Count);
                    var decaySkill = skillsPossibleToDecay[index];
                    dbPlayer.Skills[decaySkill].XP = 0;
                    dbPlayer.Skills[decaySkill].Rank--;

                    if(!modifiedSkills.Contains(decaySkill))
                        modifiedSkills.Add(decaySkill);

                    if (dbPlayer.Skills[decaySkill].Rank <= 0)
                        skillsPossibleToDecay.Remove(decaySkill);

                    if(!decayedSkills.Contains(decaySkill))
                        decayedSkills.Add(decaySkill);
                }
            }

            // Safety check - Any excess XP over the required amount is lost.
            requiredXP = GetRequiredXP(dbPlayer.Skills[skill].Rank);
            if (dbPlayer.Skills[skill].XP > requiredXP)
            {
                dbPlayer.Skills[skill].XP = 0;
            }

            DB.Set(dbPlayer);

            modifiedSkills.Add(skill);
            Gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(modifiedSkills));

            // Send out an event signifying that a player has received a skill rank increase.
            if(receivedRankUp)
            {
                EventsPlugin.SignalEvent("SWLOR_GAIN_SKILL_POINT", player);
            }

            foreach (var decayedSkill in decayedSkills)
            {
                EventsPlugin.PushEventData("SKILL_TYPE_ID", ((int)decayedSkill).ToString());
                EventsPlugin.SignalEvent("SWLOR_SKILL_LOST_BY_DECAY", player);
            }
        }

        /// <summary>
        /// Gives the player an ability point which can be distributed to the attribute of their choice
        /// from the character menu. One point is earned per 10 skill ranks
        /// </summary>
        /// <param name="player">The player to receive the AP.</param>
        /// <param name="dbPlayer">The database entity.</param>
        private static void ApplyAbilityPoint(uint player, Player dbPlayer)
        {
            // Total AP have been earned (350SP = 35AP)
            if (dbPlayer.TotalAPAcquired >= SkillCap / 10) return;

            if (dbPlayer.TotalSPAcquired % 10 == 0)
            {
                dbPlayer.UnallocatedAP++;
                dbPlayer.TotalAPAcquired++;

                SendMessageToPC(player, ColorToken.Green("You acquired 1 ability point!"));
            }
        }

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
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

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// This prevents players from accidentally losing XP when distributing to skills at or near their maximum rank.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public static int GetMaxDistributableXP(uint player, SkillType skillType)
        {
            if (skillType == SkillType.Invalid || !GetIsPC(player) || GetIsDM(player)) 
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var skillDetails = GetSkillDetails(skillType);
            var currentSkill = dbPlayer.Skills[skillType];

            // If skill is already at maximum rank, no XP can be distributed
            if (currentSkill.Rank >= skillDetails.MaxRank)
                return 0;

            var totalDistributableXP = 0;
            var currentRank = currentSkill.Rank;
            var currentXP = currentSkill.XP;

            // Calculate XP needed to fill remaining ranks
            while (currentRank < skillDetails.MaxRank)
            {
                var requiredXPForNextRank = GetRequiredXP(currentRank);
                var xpNeededForThisRank = requiredXPForNextRank - currentXP;
                
                totalDistributableXP += xpNeededForThisRank;
                currentRank++;
                currentXP = 0; // After first rank, we start from 0 XP for subsequent ranks
            }

            return totalDistributableXP;
        }
    }
}
