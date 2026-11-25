using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>Skill XP awards and the formulas that size them. The catalog and the rank rules are on <see cref="ISkillService"/>.</summary>
    public interface ISkillXPService
    {
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
        int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false, bool applyHenchmanPenalty = true, uint xpMessageSource = OBJECT_INVALID);

        int CalculateEspionageXP(PerkType profession, int tier, int skillRank);

        /// <summary>
        /// Keeps a tiered activity useful until its next unlock, with a quarter of same-level XP as the floor.
        /// </summary>
        int GetPracticeXP(int activityLevel, int skillRank, int rankLimit);

        /// <summary>
        /// Retrieves XP for the target level minus the player's rank. Trivial activities return zero.
        /// </summary>
        int GetDeltaXP(int delta);
    }
}
