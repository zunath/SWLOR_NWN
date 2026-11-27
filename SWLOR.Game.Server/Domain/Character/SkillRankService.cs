using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;
using SkillAttribute = SWLOR.Game.Server.Domain.Foundation.SkillAttribute;
using SkillType = SWLOR.Game.Server.Domain.Foundation.SkillType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The skill rank rules: the skill cap total, each skill's rank limit and ceiling, which skills may decay when a rank is gained at the cap,
    /// and how much XP a skill can still take. They moved here from <see cref="SkillService"/>, which keeps the catalog and the chart reads.
    /// </summary>
    public class SkillRankService : ISkillRankService
    {
        private const int SkillCap = SkillService.SkillCap;

        private static readonly FrozenSet<SkillType> _skillCapContributingSkillTypes = Enum.GetValues(typeof(SkillType))
            .Cast<SkillType>()
            .Where(x => x.GetAttribute<SkillType, SkillAttribute>().ContributesToSkillCap)
            .ToFrozenSet();

        private readonly IDBService _db;
        private readonly ISkillService _skill;

        public SkillRankService(
            IDBService db,
            ISkillService skill)
        {
            _db = db;
            _skill = skill;
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
            return _skill.TryIsContributingSkill(skillType, out var contributes)
                ? contributes
                : _skillCapContributingSkillTypes.Contains(skillType);
        }

        public bool ShouldDecaySkillRank(bool contributesToSkillCap, int totalRanks)
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
        /// Only unlocked skills that count toward the skill cap and have at least one rank can decay. Of those, skills above
        /// their rank limit decay first; only when none remain do skills with no limit decay. A skill at or below its rank
        /// limit never decays.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill gaining the rank, which never decays itself.</param>
        /// <returns>The skills to pick a decay from.</returns>
        public List<SkillType> GetDecayCandidates(Player dbPlayer, SkillType skill)
        {
            var aboveLimit = new List<SkillType>();
            var withoutLimit = new List<SkillType>();

            foreach (var (type, pcSkill) in dbPlayer.Skills)
            {
                if (type == skill || pcSkill.IsLocked || pcSkill.Rank <= 0)
                    continue;

                var detail = _skill.GetSkillDetails(type);
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

                var detail = _skill.GetSkillDetails(type);
                if (!detail.ContributesToSkillCap)
                    continue;

                var limit = GetRankLimit(pcSkill, detail) ?? 0;
                capacity += Math.Max(0, pcSkill.Rank - limit);
            }

            return capacity;
        }

        /// <summary>
        /// Determines whether the skill cap blocks a skill from gaining XP: the skill counts toward the cap, the player is at
        /// or above the cap, and no other skill can decay (see <see cref="GetDecayCandidates"/>). A skill that does not count
        /// toward the cap cannot decay, so it never prevents the block, and a skill with a rank limit can decay only while its
        /// rank is above that limit.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill which would gain XP.</param>
        /// <returns>true if the skill cannot gain XP because of the skill cap, false otherwise.</returns>
        public bool IsBlockedBySkillCap(Player dbPlayer, SkillType skill)
        {
            return _skill.GetSkillDetails(skill).ContributesToSkillCap &&
                   GetTotalContributingSkillRanks(dbPlayer) >= SkillCap &&
                   GetDecayCandidates(dbPlayer, skill).Count <= 0;
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

            var skillDetails = _skill.GetSkillDetails(skillType);
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
                var requiredXPForNextRank = _skill.GetRequiredXP(currentRank);
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
    }
}
