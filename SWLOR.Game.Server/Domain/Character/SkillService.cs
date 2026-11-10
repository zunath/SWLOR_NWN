using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SkillCategoryType = SWLOR.Game.Server.Domain.Character.SkillCategoryType;
using SkillAttribute = SWLOR.Game.Server.Domain.Character.SkillAttribute;
using SkillCategoryAttribute = SWLOR.Game.Server.Domain.Character.SkillCategoryAttribute;
using SkillType = SWLOR.Game.Server.Domain.Character.SkillType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The skill catalog, XP awards, rank rules and decay: the logic and state that used to be static in
    /// the facade <c>Skill</c>, moved here unchanged. The event handlers run on the container's instance, and static
    /// callers reach it through <c>GameServices.Skill</c>.
    /// </summary>
    public class SkillService : ISkillService
    {
        private static readonly FrozenSet<SkillType> _skillCapContributingSkillTypes = Enum.GetValues(typeof(SkillType))
            .Cast<SkillType>()
            .Where(x => x.GetAttribute<SkillType, SkillAttribute>().ContributesToSkillCap)
            .ToFrozenSet();

        private readonly IDBService _db;
        private readonly IGuiService _gui;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly ITwinPistolEquipmentService _twinPistolEquipment;
        private readonly Lazy<IStatService> _stat;

        private readonly Lazy<IDroidService> _droid;

        public SkillService(
            IDBService db,
            IGuiService gui,
            IPlayerFeedbackService playerFeedback,
            ITwinPistolEquipmentService twinPistolEquipment,
            Lazy<IStatService> stat,
            Lazy<IDroidService> droid)
        {
            _droid = droid;
            _db = db;
            _gui = gui;
            _playerFeedback = playerFeedback;
            _twinPistolEquipment = twinPistolEquipment;
            _stat = stat;
            _xpMessages = new SkillXPMessageQueue(
                callback => ScheduleXPMessages(callback),
                (player, skill, xp) =>
                {
                    if (GetIsObjectValid(player) && GetIsPC(player))
                        SendMessageToPC(player, $"You earned {GetSkillDetails(skill).Name} skill experience. ({xp})");
                });
        }

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
        public int APCap { get; } = SkillCap / 10;

        public int GetTotalSkillPoints(Player dbPlayer)
        {
            var earnedSP = dbPlayer.TotalSPAcquired;
            if (earnedSP < 0)
                earnedSP = 0;

            if (earnedSP > SkillCap)
                earnedSP = SkillCap;

            return StartingSkillPoints + earnedSP;
        }

        public int GetTotalContributingSkillRanks(Player dbPlayer)
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

        private bool IsSkillCapContributingSkill(SkillType skillType)
        {
            return _allSkillsContributingToCap.Count > 0
                ? _allSkillsContributingToCap.ContainsKey(skillType)
                : _skillCapContributingSkillTypes.Contains(skillType);
        }

        private bool ShouldDecaySkillRank(bool contributesToSkillCap, int totalRanks)
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
        public int? GetRankLimit(PlayerSkill pcSkill, SkillAttribute details)
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
        public int GetRankCeiling(PlayerSkill pcSkill, SkillAttribute details)
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
        private List<SkillType> GetDecayCandidates(Player dbPlayer, SkillType skill)
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
        private int GetDecayCapacity(Player dbPlayer, SkillType skill)
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
        public bool IsBlockedBySkillCap(Player dbPlayer, SkillType skill)
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

            var modifiedSkills = new List<SkillType>();
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var details = GetSkillDetails(skill);
            var pcSkill = dbPlayer.Skills[skill];
        /// <summary>
        /// Gets the amount of XP required to reach the next level.
        /// </summary>
        /// <param name="level">The level to use for the search.</param>
        /// <returns>The amount of XP required to reach the next level.</returns>
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
                _db.Set(dbPlayer);
                return debtRemoved;
            }

            // Maxed skills and skills at the player's rank limit can repay XP debt,
            // but cannot earn XP or be blocked by the total-rank cap.
            var rankCeiling = GetRankCeiling(pcSkill, details);
            if (pcSkill.Rank >= rankCeiling)
            {
                var clearedXP = pcSkill.XP > 0;
                pcSkill.XP = 0;
                _db.Set(dbPlayer);

                // A limit can be set while the skill has progress; show the cleared progress in an open Skills window.
                if (clearedXP)
                    _gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(new List<SkillType> { skill }));

                // Languages gain XP from overheard speech, so a limited language would warn whenever someone speaks it.
                if (rankCeiling < details.MaxRank && details.ContributesToSkillCap)
                    _playerFeedback.SendWarningToPlayer(player, "SKILL_LIMIT", ColorToken.Red($"{details.Name} is at your limit of {rankCeiling}. Raise it in the Skills window."));

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
                    _db.Set(dbPlayer);

                _playerFeedback.SendWarningToPlayer(player, "SKILL_CAP", ColorToken.Red($"You cannot gain {details.Name} XP. You are at the skill cap of {SkillCap} and all of your other skills are locked or at their limits. Unlock a skill or change a limit in the Skills menu to resume gaining {details.Name} XP."));
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

            _db.Set(dbPlayer);

            modifiedSkills.Add(skill);
            _gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(modifiedSkills));

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
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void AddMissingSkills()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            foreach (var skill in GetAllSkills())
            {
                if (!dbPlayer.Skills.ContainsKey(skill.Key))
                {
                    dbPlayer.Skills[skill.Key] = new PlayerSkill();
                }
            }

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// This prevents players from accidentally losing XP when distributing to skills at or near their maximum rank or rank limit.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public int GetMaxDistributableXP(uint player, SkillType skillType)
        {
            if (skillType == SkillType.Invalid || !GetIsPC(player) || GetIsDM(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            return GetMaxDistributableXP(dbPlayer, skillType);
        }

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// No XP can be distributed to a skill at its max rank or rank limit, or while the skill cap blocks it.
        /// </summary>
        /// <param name="dbPlayer">The player entity</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public int GetMaxDistributableXP(Player dbPlayer, SkillType skillType)
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

        public int GetCreatureSkillRank(uint creature, SkillType skillType)
        {
            if (GetIsDM(creature) || skillType == SkillType.Invalid)
                return 0;

            if (IsDroid(creature))
            {
                return GetDroidSkillRank(creature, skillType);
            }

            if (!GetIsPC(creature))
                return 0;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = _db.Get<Player>(playerId);

            return dbPlayer.Skills.TryGetValue(skillType, out var skill)
                ? skill.Rank
                : 0;
        }

        // All categories, including inactive
        private readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _allCategories = new();
        private readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _allCategoriesWithSkillContributing = new();

        // Active categories only
        private readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _activeCategories = new();
        private readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _activeCategoriesWithSkillContributing = new();

        // All skills, including inactive
        private readonly Dictionary<SkillType, SkillAttribute> _allSkills = new();
        private readonly Dictionary<SkillCategoryType, List<SkillType>> _allSkillsByCategory = new();
        private readonly Dictionary<SkillType, SkillAttribute> _allSkillsContributingToCap = new();

        // Active skills only
        private readonly Dictionary<SkillType, SkillAttribute> _activeSkills = new();
        private readonly Dictionary<SkillCategoryType, List<SkillType>> _activeSkillsByCategory = new();
        private readonly Dictionary<SkillType, SkillAttribute> _activeSkillsContributingToCap = new();
        private readonly Dictionary<SkillType, SkillAttribute> _activeCraftingSkills = new();
        private readonly Dictionary<SkillType, SkillAttribute> _activeResearchableCraftingSkills = new();
        private readonly List<SkillType> _activeSkillsForDisplay = new();
        private readonly Dictionary<SkillCategoryType, List<SkillType>> _activeSkillsByCategoryForDisplay = new();

        /// <summary>
        /// When the module loads, skills and categories are organized into dictionaries for quick look-ups later on.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            // Initialize the list of categories.
            var categories = Enum.GetValues(typeof(SkillCategoryType)).Cast<SkillCategoryType>();
            foreach (var category in categories)
            {
                var categoryDetail = category.GetAttribute<SkillCategoryType, SkillCategoryAttribute>();
                _allCategories[category] = categoryDetail;
                _allSkillsByCategory[category] = new List<SkillType>();

                if (categoryDetail.IsActive)
                {
                    _activeSkillsByCategory[category] = new List<SkillType>();
                }
            }

            // Organize skills to make later reads quicker.
            var skills = Enum.GetValues(typeof(SkillType)).Cast<SkillType>();
            foreach (var skillType in skills)
            {
                var skillDetail = skillType.GetAttribute<SkillType, SkillAttribute>();
                var categoryDetail = _allCategories[skillDetail.Category];

                // Add to the skills cache
                _allSkills[skillType] = skillDetail;

                // Add to contributing cache if the skill contributes towards the skill cap.
                if (skillDetail.ContributesToSkillCap)
                {
                    _allSkillsContributingToCap[skillType] = skillDetail;
                    _allCategoriesWithSkillContributing[skillDetail.Category] = categoryDetail;

                    if (categoryDetail.IsActive)
                    {
                        _activeCategoriesWithSkillContributing[skillDetail.Category] = categoryDetail;
                    }

                    if (skillDetail.IsActive)
                    {
                        _activeSkillsContributingToCap[skillType] = skillDetail;
                    }
                }

                // Add to active cache if the skill is active
                if (skillDetail.IsActive)
                {
                    _activeSkills[skillType] = skillDetail;

                    if(!_activeSkillsByCategory.ContainsKey(skillDetail.Category))
                        _activeSkillsByCategory[skillDetail.Category] = new List<SkillType>();

                    _activeSkillsByCategory[skillDetail.Category].Add(skillType);

                    if (skillDetail.IsShownInCraftMenu)
                    {
                        _activeCraftingSkills[skillType] = skillDetail;
                    }

                    if (skillDetail.IsShownInResearchMenu)
                    {
                        _activeResearchableCraftingSkills[skillType] = skillDetail;
                    }
                }

                // Add to active category cache if the skill and category are both active.
                if (skillDetail.IsActive && categoryDetail.IsActive)
                {
                    _activeCategories[skillDetail.Category] = categoryDetail;
                }

                // Add to the skills by category cache.
                _allSkillsByCategory[skillDetail.Category].Add(skillType);
            }

            OrganizeSkillsForDisplay();

            EventsPlugin.SignalEvent("SWLOR_CACHE_SKILLS_LOADED", GetModule());
            Console.WriteLine($"Loaded {_activeCategories.Count} skill categories.");
            Console.WriteLine($"Loaded {_allSkills.Count} skills.");
        }

        private void OrganizeSkillsForDisplay()
        {
            _activeSkillsForDisplay.Clear();
            _activeSkillsByCategoryForDisplay.Clear();

            foreach (var (category, skills) in _activeSkillsByCategory)
            {
                _activeSkillsByCategoryForDisplay[category] = skills
                    .OrderBy(skill => _activeSkills[skill].Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(skill => (int)skill)
                    .ToList();
            }

            var nonLanguageSkills = _activeSkills
                .Where(skill => skill.Value.Category != SkillCategoryType.Languages)
                .OrderBy(skill => skill.Value.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(skill => (int)skill.Key)
                .Select(skill => skill.Key);
            var languageSkills = _activeSkills
                .Where(skill => skill.Value.Category == SkillCategoryType.Languages)
                .OrderBy(skill => skill.Value.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(skill => (int)skill.Key)
                .Select(skill => skill.Key);

            _activeSkillsForDisplay.AddRange(nonLanguageSkills);
            _activeSkillsForDisplay.AddRange(languageSkills);
        }

        /// <summary>
        /// Retrieves a list of all skills, including inactive ones.
        /// </summary>
        /// <returns>A list of all skills.</returns>
        public Dictionary<SkillType, SkillAttribute> GetAllSkills()
        {
            return _allSkills.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all skills, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skills.</returns>
        public Dictionary<SkillType, SkillAttribute> GetAllActiveSkills()
        {
            return _activeSkills.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a display-ordered list of all active skills.
        /// </summary>
        /// <returns>A display-ordered list of active skills.</returns>
        public Dictionary<SkillType, SkillAttribute> GetAllActiveSkillsForDisplay()
        {
            return _activeSkillsForDisplay.ToDictionary(x => x, y => _activeSkills[y]);
        }

        /// <summary>
        /// Retrieves a list of all skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skills contributing towards the skill cap.</returns>
        public Dictionary<SkillType, SkillAttribute> GetAllContributingSkills()
        {
            return _allSkillsContributingToCap.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of active skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of active skills contributing towards the skill cap.</returns>
        public Dictionary<SkillType, SkillAttribute> GetActiveContributingSkills()
        {
            return _activeSkillsContributingToCap.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all skill categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all skill categories</returns>
        public Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllSkillCategories()
        {
            return _allCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the crafting menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the crafting menu.</returns>
        public Dictionary<SkillType, SkillAttribute> GetActiveCraftingSkills()
        {
            return _activeCraftingSkills.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the research menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the research menu.</returns>
        public Dictionary<SkillType, SkillAttribute> GetActiveResearchableCraftingSkills()
        {
            return _activeResearchableCraftingSkills.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all skill categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skill categories.</returns>
        public Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllActiveSkillCategories()
        {
            return _activeCategories.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of all skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categories which have skills that contribute towards the skill cap.</returns>
        public Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllContributingSkillCategories()
        {
            return _allCategoriesWithSkillContributing.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a list of active skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categores which have skills that contribute towards the skill cap.</returns>
        public Dictionary<SkillCategoryType, SkillCategoryAttribute> GetActiveContributingSkillCategories()
        {
            return _activeCategoriesWithSkillContributing.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves all skills by a given category, including inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing skills in the specified category.</returns>
        public Dictionary<SkillType, SkillAttribute> GetAllSkillsByCategory(SkillCategoryType category)
        {
            return _allSkillsByCategory[category].ToDictionary(x => x, y => _allSkills[y]);
        }

        /// <summary>
        /// Retrieves active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing active skills in the specified category.</returns>
        public Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategory(SkillCategoryType category)
        {
            return _activeSkillsByCategory[category].ToDictionary(x => x, y => _activeSkills[y]);
        }

        /// <summary>
        /// Retrieves display-ordered active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing display-ordered active skills in the specified category.</returns>
        public Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategoryForDisplay(SkillCategoryType category)
        {
            return _activeSkillsByCategoryForDisplay[category].ToDictionary(x => x, y => _activeSkills[y]);
        }

        /// <summary>
        /// Retrieves details about a specific skill.
        /// </summary>
        /// <param name="skillType">The skill whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill.</returns>
        public SkillAttribute GetSkillDetails(SkillType skillType)
        {
            return _allSkills[skillType];
        }

        /// <summary>
        /// Retrieves details about a specific skill category.
        /// </summary>
        /// <param name="category">The category whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill category.</returns>
        public SkillCategoryAttribute GetSkillCategoryDetails(SkillCategoryType category)
        {
            return _allCategories[category];
        }

        private readonly Dictionary<BaseItem, SkillType> _itemToSkillMapping = new();

        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadMappings()
        {
            LoadItemToSkillMapping();
        }

        /// <summary>
        /// Loads the base item -> skill type mappings.
        /// </summary>
        private void LoadItemToSkillMapping()
        {
            // Vibroblade
            _itemToSkillMapping[BaseItem.BastardSword] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.BattleAxe] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.Katana] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.Longsword] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.Scimitar] = SkillType.Vibroblade;

            // Vibroknife
            _itemToSkillMapping[BaseItem.Dagger] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.HandAxe] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Kama] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Kukri] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Rapier] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.ShortSword] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Sickle] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Whip] = SkillType.Vibroknife;

            // Lightsaber
            _itemToSkillMapping[BaseItem.Lightsaber] = SkillType.Lightsaber;
            _itemToSkillMapping[BaseItem.Electroblade] = SkillType.Lightsaber;

            // Heavy Vibroblade
            _itemToSkillMapping[BaseItem.DireMace] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.DwarvenWarAxe] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.GreatAxe] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.GreatSword] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.HeavyFlail] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.WarHammer] = SkillType.HeavyVibroblade;

            // Spear
            _itemToSkillMapping[BaseItem.Halberd] = SkillType.Spear;
            _itemToSkillMapping[BaseItem.Scythe] = SkillType.Spear;
            _itemToSkillMapping[BaseItem.ShortSpear] = SkillType.Spear;
            _itemToSkillMapping[BaseItem.Trident] = SkillType.Spear;

            // Twin Blade
            _itemToSkillMapping[BaseItem.TwoBladedSword] = SkillType.TwinBlade;
            _itemToSkillMapping[BaseItem.DoubleAxe] = SkillType.TwinBlade;

            // Saberstaff
            _itemToSkillMapping[BaseItem.Saberstaff] = SkillType.Saberstaff;
            _itemToSkillMapping[BaseItem.TwinElectroBlade] = SkillType.Saberstaff;

            // Katar
            _itemToSkillMapping[BaseItem.Bracer] = SkillType.Katar;
            _itemToSkillMapping[BaseItem.Gloves] = SkillType.Katar;
            _itemToSkillMapping[BaseItem.Katar] = SkillType.Katar;

            // Power Glove
            _itemToSkillMapping[BaseItem.PowerGlove] = SkillType.Unarmed;

            // Staff
            _itemToSkillMapping[BaseItem.Club] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.LightFlail] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.LightHammer] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.LightMace] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.MorningStar] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.QuarterStaff] = SkillType.Staff;

            // Pistol
            _itemToSkillMapping[BaseItem.Pistol] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.LegacyPistol] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.Arrow] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.Bullet] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.Sling] = SkillType.Pistol;

            // Simulated left-hand pistol profile. Equipped behavior uses GetWeaponSkill.
            _itemToSkillMapping[BaseItem.CompanionPistol] = SkillType.TwinPistol;

            // Rifle
            _itemToSkillMapping[BaseItem.Cannon] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Rifle] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Longbow] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Bolt] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Grenade] = SkillType.Rifle;

            // Throwing
            _itemToSkillMapping[BaseItem.Shuriken] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.ThrowingAxe] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.Dart] = SkillType.Throwing;

            Console.WriteLine($"Loaded {_itemToSkillMapping.Count} item to skill mappings.");
        }

        /// <summary>
        /// Retrieves the skill type associated with a base item type.
        /// If no skill is associated with the item, SkillType.Invalid will be returned.
        /// </summary>
        /// <param name="baseItem">The type of base item to look for.</param>
        /// <returns>A skill type associated with the given base item type.</returns>
        public SkillType GetSkillTypeByBaseItem(BaseItem baseItem)
        {
            if (!_itemToSkillMapping.ContainsKey(baseItem))
                return SkillType.Invalid;

            return _itemToSkillMapping[baseItem];
        }

        /// <summary>
        /// Retrieves the skill an attack made with an item of this base type trains and rolls against.
        /// Arms-slot gloves and bracers are the engine's attack item for bare-handed attacks, so they
        /// resolve to Unarmed here while their stored requirement skill stays unchanged.
        /// </summary>
        public SkillType GetAttackSkillTypeByBaseItem(BaseItem baseItem)
        {
            if (baseItem is BaseItem.Gloves or BaseItem.Bracer)
                return SkillType.Unarmed;

            return GetSkillTypeByBaseItem(baseItem);
        }

        /// <summary>Resolves equipped attack identity without rewriting a weapon's stored requirements.</summary>
        public SkillType GetWeaponSkill(uint creature, uint weapon, SkillType? explicitOverride = null)
        {
            if (explicitOverride.HasValue && explicitOverride != SkillType.Invalid)
                return explicitOverride.Value;
            if (!GetIsObjectValid(weapon)) return SkillType.Invalid;

            var weaponType = GetBaseItemType(weapon);
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var off = GetItemInSlot(InventorySlot.LeftHand, creature);
            if ((weapon == main || weapon == off) && _twinPistolEquipment.IsActivePair(creature))
                return SkillType.TwinPistol;
            return weaponType == BaseItem.CompanionPistol ? SkillType.Invalid : GetAttackSkillTypeByBaseItem(weaponType);
        }

        private readonly SkillXPMessageQueue _xpMessages;

        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access. The chart is a constant table
        /// built when <see cref="SkillXPChart"/> loads, so there is nothing left to build here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheXPChartData()
        {
            RuntimeHelpers.RunClassConstructor(typeof(SkillXPChart).TypeHandle);
        }

        public int GetRequiredXP(int level)
        {
            return SkillXPChart.GetRequiredXP(level);
        }

        /// <summary>
        /// Gets the total amount of XP attained at this level, excluding the XP needed to reach the next level.
        /// </summary>
        /// <param name="level">The level to retrieve.</param>
        /// <returns>The total amount of XP attained at this level</returns>
        public int GetTotalRequiredXP(int level)
        {
            return SkillXPChart.GetTotalRequiredXP(level);
        }

        /// <summary>
        /// Gets the highest level by a total XP amount, returning the remainder XP as the second item.
        /// </summary>
        /// <param name="totalXP">The total XP gained</param>
        /// <returns>A tuple containing the level and a remainder amount of XP</returns>
        public (int, int) GetLevelByTotalXP(int totalXP)
        {
            return SkillXPChart.GetLevelByTotalXP(totalXP);
        }

        /// <summary>
        /// Skill rank required to unlock a tier of an Espionage profession.
        /// </summary>
        public int GetEspionageRequiredRank(PerkType profession, int tier)
        {
            return SkillXPChart.GetEspionageRequiredRank(profession, tier);
        }

        /// <summary>
        /// Exclusive training limit: the next tier's unlock, or the Espionage skill cap.
        /// </summary>
        public int GetEspionagePracticeRankLimit(PerkType profession, int tier)
        {
            return SkillXPChart.GetEspionagePracticeRankLimit(profession, tier);
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

        /// <summary>Whether the creature is a droid (the unconverted <c>Droid</c>).</summary>
        protected virtual bool IsDroid(uint creature)
        {
            return _droid.Value.IsDroid(creature);
        }

        /// <summary>The rank a droid's controller item gives in a skill, 0 when it has none (the unconverted <c>Droid</c>).</summary>
        protected virtual int GetDroidSkillRank(uint creature, SkillType skillType)
        {
            var controller = _droid.Value.GetControllerItem(creature);
            var droidDetails = _droid.Value.LoadDroidItemPropertyDetails(controller);

            return droidDetails.Skills.TryGetValue(skillType, out var droidSkillRank)
                ? droidSkillRank
                : 0;
        }

        /// <summary>Queues a callback for the next frame (the scheduler, which runs on the real clock).</summary>
        protected virtual void ScheduleXPMessages(Action callback)
        {
            Scheduler.Schedule(callback, TimeSpan.Zero);
        }
    }
}
