using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>The skill rank rules: the cap total, rank limits and ceilings, decay and the XP a skill can still take. The catalog is on <see cref="ISkillService"/>.</summary>
    public interface ISkillRankService
    {
        int GetTotalContributingSkillRanks(Player dbPlayer);

        /// <summary>
        /// Retrieves the rank limit a player set on a skill, or null when the skill has none.
        /// Every skill can be limited, languages included. Only skills which count toward the skill cap decay.
        /// </summary>
        /// <param name="pcSkill">The player's skill record.</param>
        /// <param name="details">The skill's details.</param>
        /// <returns>The rank limit, clamped to the skill's rank range, or null.</returns>
        int? GetRankLimit(PlayerSkill pcSkill, SkillAttribute details);

        /// <summary>
        /// Retrieves the rank at which a skill stops gaining XP: its rank limit, or its max rank when it has no limit.
        /// </summary>
        /// <param name="pcSkill">The player's skill record.</param>
        /// <param name="details">The skill's details.</param>
        /// <returns>The highest rank the skill can reach.</returns>
        int GetRankCeiling(PlayerSkill pcSkill, SkillAttribute details);

        /// <summary>
        /// Retrieves the skills which may lose a rank to decay when another skill gains a rank at the skill cap.
        /// Only unlocked skills that count toward the skill cap and have at least one rank can decay. Of those, skills above
        /// their rank limit decay first; only when none remain do skills with no limit decay. A skill at or below its rank
        /// limit never decays.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill gaining the rank, which never decays itself.</param>
        /// <returns>The skills to pick a decay from.</returns>
        List<SkillType> GetDecayCandidates(Player dbPlayer, SkillType skill);

        bool ShouldDecaySkillRank(bool contributesToSkillCap, int totalRanks);

        /// <summary>
        /// Determines whether the skill cap blocks a skill from gaining XP: the skill counts toward the cap, the player is at
        /// or above the cap, and no other skill can decay (see <see cref="GetDecayCandidates"/>). A skill that does not count
        /// toward the cap cannot decay, so it never prevents the block, and a skill with a rank limit can decay only while its
        /// rank is above that limit.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="skill">The skill which would gain XP.</param>
        /// <returns>true if the skill cannot gain XP because of the skill cap, false otherwise.</returns>
        bool IsBlockedBySkillCap(Player dbPlayer, SkillType skill);

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// This prevents players from accidentally losing XP when distributing to skills at or near their maximum rank or rank limit.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        int GetMaxDistributableXP(uint player, SkillType skillType);

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// No XP can be distributed to a skill at its max rank or rank limit, or while the skill cap blocks it.
        /// </summary>
        /// <param name="dbPlayer">The player entity</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        int GetMaxDistributableXP(Player dbPlayer, SkillType skillType);
    }
}
