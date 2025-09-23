using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Skill.Contracts
{
    public interface ISkillService
    {
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
        /// When the module loads, cache all XP chart data used for quick access.
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
        /// Retrieves the base XP amount by the delta of a player's skill rank versus the target's level.
        /// If delta is above the highest delta, the highest delta will be used.
        /// If delta is lower than the lowest delta, zero will be returned.
        /// </summary>
        /// <param name="delta">The delta to compare.</param>
        /// <returns>The base XP amount based on the delta. Returns 0 if delta is below the lowest.</returns>
        int GetDeltaXP(int delta);

        /// <summary>
        /// This is the maximum number of AP a single character can earn in total. This must be evenly divisible into SkillCap.
        /// </summary>
        int APCap { get; }

        /// <summary>
        /// Gives XP towards a specific skill to a player.
        /// </summary>
        /// <param name="player">The player to give XP to.</param>
        /// <param name="skill">The type of skill to give XP towards.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="ignoreBonuses">If true, bonuses from food and other sources will NOT be applied.</param>
        /// <param name="applyHenchmanPenalty">If true, a penalty will apply if the player has a henchman active (droid, pet, etc.)</param>
        void GiveSkillXP(
            uint player,
            SkillType skill,
            int xp,
            bool ignoreBonuses = false,
            bool applyHenchmanPenalty = true);

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        void AddMissingSkills();

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// This prevents players from accidentally losing XP when distributing to skills at or near their maximum rank.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        int GetMaxDistributableXP(uint player, SkillType skillType);

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
        /// <returns>A list of skill categories which have skills that contribute towards the skill cap.</returns>
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
    }
}
