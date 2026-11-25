using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SkillAttribute = SWLOR.Game.Server.Domain.Foundation.SkillAttribute;
using SkillType = SWLOR.Game.Server.Domain.Foundation.SkillType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Skill XP awards: <see cref="GiveSkillXP"/> with its bonuses, debt repayment, rank-ups and decay, the three XP formulas, the
    /// combined XP message queue and the once-per-login rank limit warnings. They moved here from <see cref="SkillService"/>, which keeps the
    /// catalog and the rank rules.
    /// </summary>
    public class SkillXPService : ISkillXPService
    {
        private const int SkillCap = SkillService.SkillCap;

        /// <summary>
        /// Skills whose rank limit warning a player has seen since logging in. A limited skill keeps
        /// receiving XP awards, so the warning explains why XP stopped once and then stays quiet.
        /// </summary>
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly HashSet<(string PlayerId, SkillType Skill)> _limitWarningsSent = new();

        [ReloadState(ReloadStateKind.Runtime)]
        private readonly SkillXPMessageQueue _xpMessages;

        private readonly IDBService _db;
        private readonly IGuiRefreshPublisher _gui;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly Lazy<IStatService> _stat;
        private readonly ISkillService _skill;

        public SkillXPService(
            IDBService db,
            IGuiRefreshPublisher gui,
            IPlayerFeedbackService playerFeedback,
            Lazy<IStatService> stat,
            ISkillService skill)
        {
            _db = db;
            _gui = gui;
            _playerFeedback = playerFeedback;
            _stat = stat;
            _skill = skill;
            _xpMessages = new SkillXPMessageQueue(
                callback => ScheduleXPMessages(callback),
                (player, skill, xp) =>
                {
                    if (GetIsObjectValid(player) && GetIsPC(player))
                        SendMessageToPC(player, $"You earned {_skill.GetSkillDetails(skill).Name} skill experience. ({xp})");
                });
        }

        /// <summary>
        /// Gives XP towards a specific skill to a player.
        /// </summary>
        /// <param name="player">The player to give XP to.</param>
        /// <param name="skill">The type of skill to give XP towards.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="ignoreBonuses">If true, bonuses from food and other sources will NOT be applied.</param>
        /// <param name="applyHenchmanPenalty">If true, the 30% henchman penalty is applied to the award. It is applied unconditionally: nothing checks whether the player has a henchman active (droid, pet, etc.), so callers pass false to skip it.</param>
        /// <param name="xpMessageSource">Combines XP feedback for the same player, skill, and source until the next frame.</param>
        /// <returns>
        /// The XP the award used: XP debt repaid plus XP applied to the skill, after bonuses and penalties.
        /// XP discarded at a max rank or rank limit, or blocked by the skill cap, is not counted.
        /// </returns>
        public int GiveSkillXP(
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

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var details = _skill.GetSkillDetails(skill);
            var pcSkill = dbPlayer.Skills[skill];
            var requiredXP = _skill.GetRequiredXP(pcSkill.Rank);

            xp = ApplyXPBonuses(player, dbPlayer, xp, ignoreBonuses, applyHenchmanPenalty);

            var debtRemoved = RepayXPDebt(player, dbPlayer, ref xp);

            if (xp <= 0)
            {
                _db.Set(dbPlayer);
                return debtRemoved;
            }

            // Maxed skills and skills at the player's rank limit can repay XP debt,
            // but cannot earn XP or be blocked by the total-rank cap.
            var rankCeiling = _skill.GetRankCeiling(pcSkill, details);
            if (pcSkill.Rank >= rankCeiling)
            {
                ClearXPAtRankCeiling(player, dbPlayer, skill, pcSkill, details, rankCeiling);
                return debtRemoved;
            }

            var totalRanks = dbPlayer.Skills
                .Where(x =>
                {
                    var detail = _skill.GetSkillDetails(x.Key);
                    return detail.ContributesToSkillCap;
                })
                .Sum(x => x.Value.Rank);
            var skillsPossibleToDecay = _skill.GetDecayCandidates(dbPlayer, skill);

            // If player is at the skill cap and no skills are available for decay, exit early.
            // Share the warning limit across skills and overflow: one kill can award several skills,
            // but they are blocked by the same total-rank cap and require the same unlock action.
            if (details.ContributesToSkillCap && skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
            {
                // Debt repaid by this award stays repaid.
                if (debtRemoved > 0)
                    _db.Set(dbPlayer);

                _playerFeedback.SendWarningToPlayer(player, "SKILL_CAP", ColorToken.Red($"You cannot gain {details.Name} XP. You are at the skill cap of {SkillCap} and all of your other skills are locked or at their limits. Unlock a skill or change a limit in the Skills menu to resume gaining {details.Name} XP."));
                return debtRemoved;
            }

            if (xpMessageSource == OBJECT_INVALID)
                SendMessageToPC(player, $"You earned {details.Name} skill experience. ({xp})");
            else
                _xpMessages.Add(player, xpMessageSource, skill, xp);

            var startingXP = pcSkill.XP;
            pcSkill.XP += xp;

            var outcome = AwardRanks(player, dbPlayer, skill, pcSkill, details, requiredXP, rankCeiling, totalRanks, skillsPossibleToDecay);

            // Safety check - Any excess XP over the required amount is lost.
            requiredXP = _skill.GetRequiredXP(dbPlayer.Skills[skill].Rank);
            if (dbPlayer.Skills[skill].XP > requiredXP)
            {
                dbPlayer.Skills[skill].XP = 0;
            }

            _db.Set(dbPlayer);

            outcome.ModifiedSkills.Add(skill);
            _gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(outcome.ModifiedSkills));

            // Send out an event signifying that a player has received a skill rank increase.
            if(outcome.ReceivedRankUp)
            {
                EventsPlugin.SignalEvent("SWLOR_GAIN_SKILL_POINT", player);
            }

            foreach (var decayedSkill in outcome.DecayedSkills)
            {
                EventsPlugin.PushEventData("SKILL_TYPE_ID", ((int)decayedSkill).ToString());
                EventsPlugin.SignalEvent("SWLOR_SKILL_LOST_BY_DECAY", player);
            }

            // Discarded XP, including progress the skill already had, is not counted as used.
            var xpApplied = Math.Max(0, outcome.XPSpentOnRanks + dbPlayer.Skills[skill].XP - startingXP);
            return debtRemoved + xpApplied;
        }

        /// <summary>What the rank loop of an award did: whether a rank was gained, the XP it spent, and the skills it changed.</summary>
        private sealed class RankUpOutcome
        {
            public bool ReceivedRankUp;
            public int XPSpentOnRanks;
            public readonly List<SkillType> ModifiedSkills = new();
            public readonly List<SkillType> DecayedSkills = new();
        }

        private int ApplyXPBonuses(uint player, Player dbPlayer, int xp, bool ignoreBonuses, bool applyHenchmanPenalty)
        {
            var bonusPercentage = 0f;

            if (!ignoreBonuses)
            {
                // Bonus for positive Social modifier.
                var social = GetAbilityScore(player, AbilityType.Social);
                if (social > 0)
                    bonusPercentage += social * 0.025f;

                // Status bonus
                bonusPercentage += GetExperiencePercentAdjustment(player) * 0.01f;

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

            return xp;
        }

        private int RepayXPDebt(uint player, Player dbPlayer, ref int xp)
        {
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

            return debtRemoved;
        }

        private void ClearXPAtRankCeiling(uint player, Player dbPlayer, SkillType skill, PlayerSkill pcSkill, SkillAttribute details, int rankCeiling)
        {
            var clearedXP = pcSkill.XP > 0;
            pcSkill.XP = 0;
            _db.Set(dbPlayer);

            // A limit can be set while the skill has progress; show the cleared progress in an open Skills window.
            if (clearedXP)
                _gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(new List<SkillType> { skill }));

            // Languages gain XP from overheard speech, so a limited language would warn whenever someone speaks it.
            // Warn once per skill per login; the once-per-login set replaces the shared timed limit.
            if (rankCeiling < details.MaxRank && details.ContributesToSkillCap &&
                _limitWarningsSent.Add((GetObjectUUID(player), skill)))
                _playerFeedback.SendWarningToPlayer(player, "SKILL_LIMIT", ColorToken.Red($"{details.Name} is at your limit of {rankCeiling}. Raise it in the Skills window."), intervalSeconds: 0);
        }

        private RankUpOutcome AwardRanks(
            uint player,
            Player dbPlayer,
            SkillType skill,
            PlayerSkill pcSkill,
            SkillAttribute details,
            int requiredXP,
            int rankCeiling,
            int totalRanks,
            List<SkillType> skillsPossibleToDecay)
        {
            var outcome = new RankUpOutcome();
            var modifiedSkills = outcome.ModifiedSkills;
            var decayedSkills = outcome.DecayedSkills;

            while (pcSkill.XP >= requiredXP)
            {
                if (details.ContributesToSkillCap && skillsPossibleToDecay.Count <= 0 && totalRanks >= SkillCap)
                    break;

                outcome.ReceivedRankUp = true;
                pcSkill.XP -= requiredXP;
                outcome.XPSpentOnRanks += requiredXP;

                if (dbPlayer.TotalSPAcquired < SkillCap && details.ContributesToSkillCap)
                {
                    dbPlayer.UnallocatedSP++;
                    dbPlayer.TotalSPAcquired++;
                }

                pcSkill.Rank++;
                FloatingTextStringOnCreature($"Your {details.Name} skill level increased to rank {pcSkill.Rank}!", player, false);

                requiredXP = _skill.GetRequiredXP(pcSkill.Rank);

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
                        var detail = _skill.GetSkillDetails(x.Key);
                        return detail.ContributesToSkillCap;
                    })
                    .Sum(x => x.Value.Rank);

                // If player is above the cap, pick a random skill out of the available decayable skills,
                // reduce its level by 1, and set XP to zero.
                if (_skill.ShouldDecaySkillRank(details.ContributesToSkillCap, totalRanks))
                {
                    // Edge case: Part of the number of levels granted cannot be given because
                    // there are no decayable skills to reduce. All excess XP is lost and we
                    // no longer need to proceed with increasing the skill rank
                    if (skillsPossibleToDecay.Count <= 0)
                    {
                        dbPlayer.Skills[skill].XP = 0;
                        _playerFeedback.SendWarningToPlayer(player, "SKILL_CAP", ColorToken.Red($"You have reached the skill cap of {SkillCap} and all of your other skills are locked or at their limits. Excess {details.Name} XP was lost."));
                        break;
                    }

                    var index = Random.Next(skillsPossibleToDecay.Count);
                    var decaySkill = skillsPossibleToDecay[index];
                    dbPlayer.Skills[decaySkill].XP = 0;
                    dbPlayer.Skills[decaySkill].Rank--;

                    if(!modifiedSkills.Contains(decaySkill))
                        modifiedSkills.Add(decaySkill);

                    // A decayed skill may have reached 0 or its limit, which can also empty the above-limit pool.
                    skillsPossibleToDecay = _skill.GetDecayCandidates(dbPlayer, skill);

                    if(!decayedSkills.Contains(decaySkill))
                        decayedSkills.Add(decaySkill);
                }
            }

            return outcome;
        }

        /// <summary>
        /// Gives the player an ability point which can be distributed to the attribute of their choice
        /// from the character menu. One point is earned per 10 skill ranks
        /// </summary>
        /// <param name="player">The player to receive the AP.</param>
        /// <param name="dbPlayer">The database entity.</param>
        private void ApplyAbilityPoint(uint player, Player dbPlayer)
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
        /// A new login shows each limited skill's warning again.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Forgets the login's rank limit warnings; a reload skips this login handler and the set starts empty, so a player online through a reload can see the warning again.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ResetLimitWarningsOnEnter()
        {
            var player = GetEnteringObject();
            if (GetIsPC(player) && !GetIsDM(player))
                ForgetLimitWarnings(GetObjectUUID(player));
        }

        /// <summary>
        /// Drops a departing player's warning records so the set does not grow until a restart.
        /// </summary>
        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets the player's rank limit warnings in the old build's memory.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ResetLimitWarningsOnExit()
        {
            var player = GetExitingObject();
            if (GetIsPC(player) && !GetIsDM(player))
                ForgetLimitWarnings(GetObjectUUID(player));
        }

        internal void ForgetLimitWarnings(string playerId)
        {
            _limitWarningsSent.RemoveWhere(entry => entry.PlayerId == playerId);
        }

        public int CalculateEspionageXP(PerkType profession, int tier, int skillRank)
        {
            return SkillXPChart.CalculateEspionageXP(profession, tier, skillRank);
        }

        /// <summary>
        /// Keeps a tiered activity useful until its next unlock, with a quarter of same-level XP as the floor.
        /// </summary>
        public int GetPracticeXP(int activityLevel, int skillRank, int rankLimit)
        {
            return SkillXPChart.GetPracticeXP(activityLevel, skillRank, rankLimit);
        }

        /// <summary>
        /// Retrieves XP for the target level minus the player's rank. Trivial activities return zero.
        /// </summary>
        public int GetDeltaXP(int delta)
        {
            return SkillXPChart.GetDeltaXP(delta);
        }

        /// <summary>The player's experience percent adjustment from status effects and gear (the unconverted <c>Stat</c>).</summary>
        protected virtual int GetExperiencePercentAdjustment(uint player)
        {
            return _stat.Value.GetStatAdjustment(player, StatType.ExperiencePercentAdjustment);
        }

        /// <summary>Queues a callback for the next frame (the scheduler, which runs on the real clock).</summary>
        protected virtual void ScheduleXPMessages(Action callback)
        {
            Scheduler.Schedule(callback, TimeSpan.Zero);
        }
    }
}
