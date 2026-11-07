using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the skill catalog, XP awards and rank rules. The logic lives in <see cref="ISkillService"/>;
    /// the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static partial class Skill
    {
        private static ISkillService Instance => FacadeAccessor.Resolve<ISkillService>();

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
        public static int APCap => Instance.APCap;

        public static int GetTotalSkillPoints(Player dbPlayer)
        {
            return Instance.GetTotalSkillPoints(dbPlayer);
        }

        public static int GetTotalContributingSkillRanks(Player dbPlayer)
        {
            return Instance.GetTotalContributingSkillRanks(dbPlayer);
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
            return Instance.GetRankLimit(pcSkill, details);
        }

        /// <summary>
        /// Retrieves the rank at which a skill stops gaining XP: its rank limit, or its max rank when it has no limit.
        /// </summary>
        /// <param name="pcSkill">The player's skill record.</param>
        /// <param name="details">The skill's details.</param>
        /// <returns>The highest rank the skill can reach.</returns>
        public static int GetRankCeiling(PlayerSkill pcSkill, SkillAttribute details)
        {
            return Instance.GetRankCeiling(pcSkill, details);
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
            return Instance.IsBlockedBySkillCap(dbPlayer, skill);
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
        public static int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false, bool applyHenchmanPenalty = true, uint xpMessageSource = OBJECT_INVALID)
        {
            return Instance.GiveSkillXP(player, skill, xp, ignoreBonuses, applyHenchmanPenalty, xpMessageSource);
        }

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddMissingSkills()
        {
            Instance.AddMissingSkills();
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
            return Instance.GetMaxDistributableXP(player, skillType);
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
            return Instance.GetMaxDistributableXP(dbPlayer, skillType);
        }

        public static int GetCreatureSkillRank(uint creature, SkillType skillType)
        {
            return Instance.GetCreatureSkillRank(creature, skillType);
        }

        /// <summary>
        /// Gets the highest level by a total XP amount, returning the remainder XP as the second item.
        /// </summary>
        /// <param name="totalXP">The total XP gained</param>
        /// <returns>A tuple containing the level and a remainder amount of XP</returns>
        public static (int, int) GetLevelByTotalXP(int totalXP)
        {
            return Instance.GetLevelByTotalXP(totalXP);
        }
    }
}
