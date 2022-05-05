using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
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
            var bonusPercentage = 0f;

            if (!ignoreBonuses)
            {
                // Bonus for positive Social modifier.
                var social = GetAbilityModifier(AbilityType.Social, player);
                if (social > 0)
                    bonusPercentage += social * 0.05f;

                // Food bonus
                var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
                if (foodEffect != null)
                {
                    bonusPercentage += foodEffect.XPBonusPercent * 0.01f;
                }

                // DM bonus
                bonusPercentage += dbPlayer.DMXPBonus * 0.01f;

                // Apply bonuses
                xp += (int)(xp * bonusPercentage);
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
