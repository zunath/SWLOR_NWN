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
        /// <summary>
        /// When the module loads, skills and categories are organized into dictionaries for quick look-ups later on.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Retrieves a list of all skills, including inactive ones.
        /// </summary>
        /// <returns>A list of all skills.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllSkills()
        {
            return Instance.GetAllSkills();
        }

        /// <summary>
        /// Retrieves a list of all skills, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skills.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllActiveSkills()
        {
            return Instance.GetAllActiveSkills();
        }

        /// <summary>
        /// Retrieves a display-ordered list of all active skills.
        /// </summary>
        /// <returns>A display-ordered list of active skills.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllActiveSkillsForDisplay()
        {
            return Instance.GetAllActiveSkillsForDisplay();
        }

        /// <summary>
        /// Retrieves a list of all skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skills contributing towards the skill cap.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllContributingSkills()
        {
            return Instance.GetAllContributingSkills();
        }

        /// <summary>
        /// Retrieves a list of active skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of active skills contributing towards the skill cap.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveContributingSkills()
        {
            return Instance.GetActiveContributingSkills();
        }

        /// <summary>
        /// Retrieves a list of all skill categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all skill categories</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllSkillCategories()
        {
            return Instance.GetAllSkillCategories();
        }

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the crafting menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the crafting menu.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveCraftingSkills()
        {
            return Instance.GetActiveCraftingSkills();
        }

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the research menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the research menu.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveResearchableCraftingSkills()
        {
            return Instance.GetActiveResearchableCraftingSkills();
        }

        /// <summary>
        /// Retrieves a list of all skill categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skill categories.</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllActiveSkillCategories()
        {
            return Instance.GetAllActiveSkillCategories();
        }

        /// <summary>
        /// Retrieves a list of all skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categories which have skills that contribute towards the skill cap.</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllContributingSkillCategories()
        {
            return Instance.GetAllContributingSkillCategories();
        }

        /// <summary>
        /// Retrieves a list of active skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categores which have skills that contribute towards the skill cap.</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetActiveContributingSkillCategories()
        {
            return Instance.GetActiveContributingSkillCategories();
        }

        /// <summary>
        /// Retrieves all skills by a given category, including inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing skills in the specified category.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllSkillsByCategory(SkillCategoryType category)
        {
            return Instance.GetAllSkillsByCategory(category);
        }

        /// <summary>
        /// Retrieves active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing active skills in the specified category.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategory(SkillCategoryType category)
        {
            return Instance.GetActiveSkillsByCategory(category);
        }

        /// <summary>
        /// Retrieves display-ordered active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing display-ordered active skills in the specified category.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategoryForDisplay(SkillCategoryType category)
        {
            return Instance.GetActiveSkillsByCategoryForDisplay(category);
        }

        /// <summary>
        /// Retrieves details about a specific skill.
        /// </summary>
        /// <param name="skillType">The skill whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill.</returns>
        public static SkillAttribute GetSkillDetails(SkillType skillType)
        {
            return Instance.GetSkillDetails(skillType);
        }

        /// <summary>
        /// Retrieves details about a specific skill category.
        /// </summary>
        /// <param name="category">The category whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill category.</returns>
        public static SkillCategoryAttribute GetSkillCategoryDetails(SkillCategoryType category)
        {
            return Instance.GetSkillCategoryDetails(category);
        }
    }
}
