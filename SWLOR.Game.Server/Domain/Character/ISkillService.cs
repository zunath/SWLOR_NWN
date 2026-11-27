using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface ISkillService
    {
        /// <summary>
        /// This is the maximum number of AP a single character can earn in total. This must be evenly divisible into SkillCap.
        /// </summary>
        int APCap { get; }

        int GetTotalSkillPoints(Player dbPlayer);

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        void AddMissingSkills();

        /// <summary>
        /// Reads whether a skill counts toward the skill cap from the catalog table, without copying it. The rank rules call this in a per-skill loop.
        /// </summary>
        /// <param name="skillType">The skill to look up.</param>
        /// <param name="contributes">Whether the skill counts toward the cap; false when the table has not been built.</param>
        /// <returns>false while the catalog table is empty (before the module cache runs), so the caller uses its own fallback; otherwise true.</returns>
        bool TryIsContributingSkill(SkillType skillType, out bool contributes);

        int GetCreatureSkillRank(uint creature, SkillType skillType);

        /// <summary>
        /// When the module loads, skills and categories are organized into dictionaries for quick look-ups later on.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Retrieves a list of all skills, including inactive ones.
        /// </summary>
        /// <returns>A list of all skills.</returns>
        Dictionary<SkillType, SkillAttribute> GetAllSkills();

        /// <summary>
        /// Retrieves a list of all skills, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skills.</returns>
        Dictionary<SkillType, SkillAttribute> GetAllActiveSkills();

        /// <summary>
        /// Retrieves a display-ordered list of all active skills.
        /// </summary>
        /// <returns>A display-ordered list of active skills.</returns>
        Dictionary<SkillType, SkillAttribute> GetAllActiveSkillsForDisplay();

        /// <summary>
        /// Retrieves a list of all skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skills contributing towards the skill cap.</returns>
        Dictionary<SkillType, SkillAttribute> GetAllContributingSkills();

        /// <summary>
        /// Retrieves a list of active skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of active skills contributing towards the skill cap.</returns>
        Dictionary<SkillType, SkillAttribute> GetActiveContributingSkills();

        /// <summary>
        /// Retrieves a list of all skill categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all skill categories</returns>
        Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllSkillCategories();

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the crafting menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the crafting menu.</returns>
        Dictionary<SkillType, SkillAttribute> GetActiveCraftingSkills();

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the research menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the research menu.</returns>
        Dictionary<SkillType, SkillAttribute> GetActiveResearchableCraftingSkills();

        /// <summary>
        /// Retrieves a list of all skill categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skill categories.</returns>
        Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllActiveSkillCategories();

        /// <summary>
        /// Retrieves a list of all skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categories which have skills that contribute towards the skill cap.</returns>
        Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllContributingSkillCategories();

        /// <summary>
        /// Retrieves a list of active skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categores which have skills that contribute towards the skill cap.</returns>
        Dictionary<SkillCategoryType, SkillCategoryAttribute> GetActiveContributingSkillCategories();

        /// <summary>
        /// Retrieves all skills by a given category, including inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing skills in the specified category.</returns>
        Dictionary<SkillType, SkillAttribute> GetAllSkillsByCategory(SkillCategoryType category);

        /// <summary>
        /// Retrieves active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing active skills in the specified category.</returns>
        Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategory(SkillCategoryType category);

        /// <summary>
        /// Retrieves display-ordered active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing display-ordered active skills in the specified category.</returns>
        Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategoryForDisplay(SkillCategoryType category);

        /// <summary>
        /// Retrieves details about a specific skill.
        /// </summary>
        /// <param name="skillType">The skill whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill.</returns>
        SkillAttribute GetSkillDetails(SkillType skillType);

        /// <summary>
        /// Retrieves details about a specific skill category.
        /// </summary>
        /// <param name="category">The category whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill category.</returns>
        SkillCategoryAttribute GetSkillCategoryDetails(SkillCategoryType category);

        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        void LoadMappings();

        /// <summary>
        /// Retrieves the skill type associated with a base item type.
        /// If no skill is associated with the item, SkillType.Invalid will be returned.
        /// </summary>
        /// <param name="baseItem">The type of base item to look for.</param>
        /// <returns>A skill type associated with the given base item type.</returns>
        SkillType GetSkillTypeByBaseItem(BaseItem baseItem);

        /// <summary>
        /// Retrieves the skill an attack made with an item of this base type trains and rolls against.
        /// Arms-slot gloves and bracers are the engine's attack item for bare-handed attacks, so they
        /// resolve to Unarmed here while their stored requirement skill stays unchanged.
        /// </summary>
        SkillType GetAttackSkillTypeByBaseItem(BaseItem baseItem);

        /// <summary>Resolves equipped attack identity without rewriting a weapon's stored requirements.</summary>
        SkillType GetWeaponSkill(uint creature, uint weapon, SkillType? explicitOverride = null);

        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access. The chart is a constant table
        /// built when <see cref="SkillXPChart"/> loads, so there is nothing left to build here.
        /// </summary>
        void CacheXPChartData();

        /// <summary>
        /// Gets the amount of XP required to reach the next level.
        /// </summary>
        /// <param name="level">The level to use for the search.</param>
        /// <returns>The amount of XP required to reach the next level.</returns>
        int GetRequiredXP(int level);

        /// <summary>
        /// Gets the total amount of XP attained at this level, excluding the XP needed to reach the next level.
        /// </summary>
        /// <param name="level">The level to retrieve.</param>
        /// <returns>The total amount of XP attained at this level</returns>
        int GetTotalRequiredXP(int level);

        /// <summary>
        /// Gets the highest level by a total XP amount, returning the remainder XP as the second item.
        /// </summary>
        /// <param name="totalXP">The total XP gained</param>
        /// <returns>A tuple containing the level and a remainder amount of XP</returns>
        (int, int) GetLevelByTotalXP(int totalXP);

        /// <summary>
        /// Skill rank required to unlock a tier of an Espionage profession.
        /// </summary>
        int GetEspionageRequiredRank(PerkType profession, int tier);

        /// <summary>
        /// Exclusive training limit: the next tier's unlock, or the Espionage skill cap.
        /// </summary>
        int GetEspionagePracticeRankLimit(PerkType profession, int tier);
    }
}
