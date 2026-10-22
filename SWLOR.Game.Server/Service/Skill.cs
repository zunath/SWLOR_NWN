using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        private static readonly Lazy<HashSet<SkillType>> _skillCapContributingSkillTypes = new(() =>
            Enum.GetValues(typeof(SkillType))
                .Cast<SkillType>()
                .Where(x => x.GetAttribute<SkillType, SkillAttribute>().ContributesToSkillCap)
                .ToHashSet());

        /// <summary>
        /// This is the maximum number of skill points a single character can have at any time.
        /// </summary>
        public const int SkillCap = 400;

        /// <summary>
        /// The base amount of SP available to every player before earned skill ranks are counted.
        /// </summary>
        public const int StartingSkillPoints = 10;

        /// <summary>
        /// Maximum spendable player SP, including the starting SP pool.
        /// </summary>
        public const int TotalSkillPointCap = SkillCap + StartingSkillPoints;

        /// <summary>
        /// This is the maximum number of AP a single character can earn in total. This must be evenly divisible into SkillCap.
        /// </summary>
        public static int APCap { get; } = SkillCap / 10;

        public static int GetTotalSkillPoints(Player dbPlayer)
        {
            var earnedSP = dbPlayer.TotalSPAcquired;
            if (earnedSP < 0)
                earnedSP = 0;

            if (earnedSP > SkillCap)
                earnedSP = SkillCap;

            return StartingSkillPoints + earnedSP;
        }

        public static int GetTotalContributingSkillRanks(Player dbPlayer)
        {
            var total = 0;
            foreach (var (skillType, skill) in dbPlayer.Skills)
            {
                if (!IsSkillCapContributingSkill(skillType))
                    continue;

                total += Math.Max(0, skill.Rank);
            }

            return total;
        }

        private static bool IsSkillCapContributingSkill(SkillType skillType)
        {
            return _allSkillsContributingToCap.Count > 0
                ? _allSkillsContributingToCap.ContainsKey(skillType)
                : _skillCapContributingSkillTypes.Value.Contains(skillType);
        }

        private static bool ShouldDecaySkillRank(bool contributesToSkillCap, int totalRanks)
        {
            return contributesToSkillCap && totalRanks > SkillCap;
        }

        /// <summary>
        /// Retrieves the rank limit a player set on a skill, or null when the skill has none.
        /// Every skill can be limited, languages included. Only skills which count toward the skill cap decay.
        /// </summary>
        /// <param name="pcSkill">The player's skill record.</param>
        /// <param name="details">The skill's details.</param>
        /// <returns>The rank limit, clamped to the skill's rank range, or null.</returns>
        public static int? GetRankLimit(PlayerSkill pcSkill, SkillAttribute details)
        {
            if (pcSkill.RankLimit == null)
                return null;

            return Math.Clamp(pcSkill.RankLimit.Value, 0, details.MaxRank);
        }

        /// <summary>
        /// Retrieves the rank at which a skill stops gaining XP: its rank limit, or its max rank when it has no limit.
        /// </summary>
        /// <param name="pcSkill">The player's skill record.</param>
        /// <param name="details">The skill's details.</param>
        /// <returns>The highest rank the skill can reach.</returns>
        public static int GetRankCeiling(PlayerSkill pcSkill, SkillAttribute details)
        {
            return GetRankLimit(pcSkill, details) ?? details.MaxRank;
        }

        /// <summary>
        /// Retrieves the skills which may lose a rank to decay when another skill gains a rank at the skill cap.
        /// Unlocked skills above their rank limit decay first. Only when none remain do unlocked skills with no limit decay.
        /// A skill at or below its rank limit never decays.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill gaining the rank, which never decays itself.</param>
        /// <returns>The skills to pick a decay from.</returns>
        private static List<SkillType> GetDecayCandidates(Player dbPlayer, SkillType skill)
        {
            var aboveLimit = new List<SkillType>();
            var withoutLimit = new List<SkillType>();

            foreach (var (type, pcSkill) in dbPlayer.Skills)
            {
                if (type == skill || pcSkill.IsLocked || pcSkill.Rank <= 0)
                    continue;

                var detail = GetSkillDetails(type);
                if (!detail.ContributesToSkillCap)
                    continue;

                var limit = GetRankLimit(pcSkill, detail);
                if (limit == null)
                    withoutLimit.Add(type);
                else if (pcSkill.Rank > limit)
                    aboveLimit.Add(type);
            }

            return aboveLimit.Count > 0 ? aboveLimit : withoutLimit;
        }

        /// <summary>
        /// Counts the ranks decay can take from other skills before none remain: each unlocked skill's ranks
        /// above its rank limit, or all of its ranks when it has no limit.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill gaining ranks, which never decays itself.</param>
        /// <returns>The number of ranks decay can take.</returns>
        private static int GetDecayCapacity(Player dbPlayer, SkillType skill)
        {
            var capacity = 0;
            foreach (var (type, pcSkill) in dbPlayer.Skills)
            {
                if (type == skill || pcSkill.IsLocked || pcSkill.Rank <= 0)
                    continue;

                var detail = GetSkillDetails(type);
                if (!detail.ContributesToSkillCap)
                    continue;

                var limit = GetRankLimit(pcSkill, detail) ?? 0;
                capacity += Math.Max(0, pcSkill.Rank - limit);
            }

            return capacity;
        }

        /// <summary>
        /// Determines whether the skill cap blocks a skill from gaining XP: the player is at the cap,
        /// and every other skill is locked, at its rank limit, or at rank 0.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill which would gain XP.</param>
        /// <returns>true if the skill cannot gain XP because of the skill cap, false otherwise.</returns>
        public static bool IsBlockedBySkillCap(Player dbPlayer, SkillType skill)
        {
            return GetSkillDetails(skill).ContributesToSkillCap &&
                   GetTotalContributingSkillRanks(dbPlayer) >= SkillCap &&
                   GetDecayCandidates(dbPlayer, skill).Count <= 0;
        }

        /// <summary>
        /// Gives XP towards a specific skill to a player.
        /// </summary>
        /// <param name="player">The player to give XP to.</param>
        /// <param name="skill">The type of skill to give XP towards.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="ignoreBonuses">If true, bonuses from food and other sources will NOT be applied.</param>
        /// <param name="applyHenchmanPenalty">If true, a penalty will apply if the player has a henchman active (droid, pet, etc.)</param>
        /// <param name="xpMessageSource">Combines XP feedback for the same player, skill, and source until the next frame.</param>
        /// <returns>
        /// The XP the award used: XP debt repaid plus XP applied to the skill, after bonuses and penalties.
        /// XP discarded at a max rank or rank limit, or blocked by the skill cap, is not counted.
        /// </returns>
        public static int GiveSkillXP(
            uint player,
            SkillType skill,
            int xp,
            bool ignoreBonuses = false,
            bool applyHenchmanPenalty = true,
            uint xpMessageSource = OBJECT_INVALID)
        {
            if (skill == SkillType.Invalid ||
                xp <= 0 ||
                !GetIsPC(player) ||
                GetIsDM(player) ||
                GetIsDead(player) ||
                GetCurrentHitPoints(player) <= 0)
                return 0;

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

                // Status bonus
                bonusPercentage += Stat.GetStatAdjustment(player, StatType.ExperiencePercentAdjustment) * 0.01f;

                // DM bonus
                bonusPercentage += dbPlayer.DMXPBonus * 0.01f;

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
                if (dbPlayer.XPDebt == 0)
                    SendMessageToPC(player, "Your XP debt has been cleared.");
                else
                    SendMessageToPC(player, $"{debtRemoved} XP was removed from your debt. (Remaining: {dbPlayer.XPDebt})");
            }

            if (xp <= 0)
            {
                DB.Set(dbPlayer);
                return debtRemoved;
            }

            // Maxed skills and skills at the player's rank limit can repay XP debt,
            // but cannot earn XP or be blocked by the total-rank cap.
            var rankCeiling = GetRankCeiling(pcSkill, details);
            if (pcSkill.Rank >= rankCeiling)
            {
                var clearedXP = pcSkill.XP > 0;
                pcSkill.XP = 0;
                DB.Set(dbPlayer);

                // A limit can be set while the skill has progress; show the cleared progress in an open Skills window.
                if (clearedXP)
                    Gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(new List<SkillType> { skill }));

                // Languages gain XP from overheard speech, so a limited language would warn whenever someone speaks it.
                if (rankCeiling < details.MaxRank && details.ContributesToSkillCap)
                    PlayerFeedback.SendWarningToPlayer(player, "SKILL_LIMIT", ColorToken.Red($"{details.Name} is at your limit of {rankCeiling}. Raise it in the Skills window."));

                return debtRemoved;
            }

            var totalRanks = dbPlayer.Skills
                .Where(x =>
                {
                    var detail = GetSkillDetails(x.Key);
                    return detail.ContributesToSkillCap;
                })
                .Sum(x => x.Value.Rank);
            var skillsPossibleToDecay = GetDecayCandidates(dbPlayer, skill);

            // If player is at the skill cap and no skills are available for decay, exit early.
            // Share the warning limit across skills and overflow: one kill can award several skills,
            // but they are blocked by the same total-rank cap and require the same unlock action.
            if (details.ContributesToSkillCap && skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
            {
                // Debt repaid by this award stays repaid.
                if (debtRemoved > 0)
                    DB.Set(dbPlayer);

                PlayerFeedback.SendWarningToPlayer(player, "SKILL_CAP", ColorToken.Red($"You cannot gain {details.Name} XP. You are at the skill cap of {SkillCap} and all of your other skills are locked or at their limits. Unlock a skill or change a limit in the Skills menu to resume gaining {details.Name} XP."));
                return debtRemoved;
            }

            if (xpMessageSource == OBJECT_INVALID)
                SendMessageToPC(player, $"You earned {details.Name} skill experience. ({xp})");
            else
                _xpMessages.Add(player, xpMessageSource, skill, xp);

            var startingXP = pcSkill.XP;
            var xpSpentOnRanks = 0;
            pcSkill.XP += xp;

            while (pcSkill.XP >= requiredXP)
            {
                if (details.ContributesToSkillCap && skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
                    break;

                receivedRankUp = true;
                pcSkill.XP -= requiredXP;
                xpSpentOnRanks += requiredXP;

                if (dbPlayer.TotalSPAcquired < SkillCap && details.ContributesToSkillCap)
                {
                    dbPlayer.UnallocatedSP++;
                    dbPlayer.TotalSPAcquired++;
                }

                pcSkill.Rank++;
                FloatingTextStringOnCreature($"Your {details.Name} skill level increased to rank {pcSkill.Rank}!", player, false);

                requiredXP = GetRequiredXP(pcSkill.Rank);

                // Leftover XP is discarded once the skill reaches its max rank or rank limit.
                if (pcSkill.Rank >= rankCeiling)
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

                // If player is above the cap, pick a random skill out of the available decayable skills,
                // reduce its level by 1, and set XP to zero.
                if (ShouldDecaySkillRank(details.ContributesToSkillCap, totalRanks))
                {
                    // Edge case: Part of the number of levels granted cannot be given because
                    // there are no decayable skills to reduce. All excess XP is lost and we
                    // no longer need to proceed with increasing the skill rank
                    if (skillsPossibleToDecay.Count <= 0)
                    {
                        dbPlayer.Skills[skill].XP = 0;
                        PlayerFeedback.SendWarningToPlayer(player, "SKILL_CAP", ColorToken.Red($"You have reached the skill cap of {SkillCap} and all of your other skills are locked or at their limits. Excess {details.Name} XP was lost."));
                        break;
                    }

                    var index = Random.Next(skillsPossibleToDecay.Count);
                    var decaySkill = skillsPossibleToDecay[index];
                    dbPlayer.Skills[decaySkill].XP = 0;
                    dbPlayer.Skills[decaySkill].Rank--;

                    if(!modifiedSkills.Contains(decaySkill))
                        modifiedSkills.Add(decaySkill);

                    // A decayed skill may have reached 0 or its limit, which can also empty the above-limit pool.
                    skillsPossibleToDecay = GetDecayCandidates(dbPlayer, skill);

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

            // Discarded XP, including progress the skill already had, is not counted as used.
            var xpApplied = Math.Max(0, xpSpentOnRanks + dbPlayer.Skills[skill].XP - startingXP);
            return debtRemoved + xpApplied;
        }

        /// <summary>
        /// Gives the player an ability point which can be distributed to the attribute of their choice
        /// from the character menu. One point is earned per 10 skill ranks
        /// </summary>
        /// <param name="player">The player to receive the AP.</param>
        /// <param name="dbPlayer">The database entity.</param>
        private static void ApplyAbilityPoint(uint player, Player dbPlayer)
        {
            // Total AP have been earned (400SP = 40AP)
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
        /// This prevents players from accidentally losing XP when distributing to skills at or near their maximum rank or rank limit.
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

            return GetMaxDistributableXP(dbPlayer, skillType);
        }

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// No XP can be distributed to a skill at its max rank or rank limit, or while the skill cap blocks it.
        /// </summary>
        /// <param name="dbPlayer">The player entity</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public static int GetMaxDistributableXP(Player dbPlayer, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var skillDetails = GetSkillDetails(skillType);
            var currentSkill = dbPlayer.Skills[skillType];
            var rankCeiling = GetRankCeiling(currentSkill, skillDetails);

            // If skill is already at maximum rank or its rank limit, no XP can be distributed
            if (currentSkill.Rank >= rankCeiling)
                return 0;

            // At the skill cap with nothing left to decay, the award would be blocked.
            if (IsBlockedBySkillCap(dbPlayer, skillType))
                return 0;

            // A skill that counts toward the cap can only gain the free ranks under the cap,
            // plus one rank for each rank decay can take from other skills.
            var reachableRank = rankCeiling;
            if (skillDetails.ContributesToSkillCap)
            {
                var freeRanks = Math.Max(0, SkillCap - GetTotalContributingSkillRanks(dbPlayer));
                reachableRank = Math.Min(rankCeiling, currentSkill.Rank + freeRanks + GetDecayCapacity(dbPlayer, skillType));
            }

            var totalDistributableXP = 0;
            var currentRank = currentSkill.Rank;
            var currentXP = currentSkill.XP;

            // Calculate XP needed to fill remaining ranks
            while (currentRank < reachableRank)
            {
                var requiredXPForNextRank = GetRequiredXP(currentRank);
                var xpNeededForThisRank = Math.Max(0, requiredXPForNextRank - currentXP);

                totalDistributableXP += xpNeededForThisRank;
                currentRank++;
                currentXP = 0; // After first rank, we start from 0 XP for subsequent ranks
            }

            // When the skill cap stopped a rank-up, the stored XP can already pay for the next rank.
            // Any award then completes that rank, so accept 1 XP to trigger it.
            if (totalDistributableXP <= 0 && reachableRank > currentSkill.Rank)
                return 1;

            return totalDistributableXP;
        }

        public static int GetCreatureSkillRank(uint creature, SkillType skillType)
        {
            if (GetIsDM(creature) || skillType == SkillType.Invalid)
                return 0;

            if (Droid.IsDroid(creature))
            {
                var controller = Droid.GetControllerItem(creature);
                var droidDetails = Droid.LoadDroidItemPropertyDetails(controller);

                return droidDetails.Skills.TryGetValue(skillType, out var droidSkillRank)
                    ? droidSkillRank
                    : 0;
            }

            if (!GetIsPC(creature))
                return 0;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);

            return dbPlayer.Skills.TryGetValue(skillType, out var skill)
                ? skill.Rank
                : 0;
        }
    }
}
