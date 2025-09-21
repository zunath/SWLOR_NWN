using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        private static readonly IGenericCacheService _cacheService = ServiceContainer.GetService<IGenericCacheService>();
        
        // Cached data
        private static IEnumCache<SkillCategoryType, SkillCategoryAttribute> _categoryCache;
        private static IEnumCache<SkillType, SkillAttribute> _skillCache;
        
        // Additional caches for complex data
        private static readonly Dictionary<SkillCategoryType, List<SkillType>> _allSkillsByCategory = new();
        private static readonly Dictionary<SkillCategoryType, List<SkillType>> _activeSkillsByCategory = new();
        
        // Pre-computed caches for fast retrieval
        private static readonly Dictionary<SkillType, SkillAttribute> _allSkills = new();
        private static readonly Dictionary<SkillType, SkillAttribute> _activeSkills = new();
        private static readonly Dictionary<SkillType, SkillAttribute> _contributingSkills = new();
        private static readonly Dictionary<SkillType, SkillAttribute> _activeContributingSkills = new();
        private static readonly Dictionary<SkillType, SkillAttribute> _craftingSkills = new();
        private static readonly Dictionary<SkillType, SkillAttribute> _researchableSkills = new();
        private static readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _allCategories = new();
        private static readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _activeCategories = new();
        private static readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _contributingCategories = new();
        private static readonly Dictionary<SkillCategoryType, SkillCategoryAttribute> _activeContributingCategories = new();
        private static readonly Dictionary<SkillCategoryType, Dictionary<SkillType, SkillAttribute>> _skillsByCategory = new();
        private static readonly Dictionary<SkillCategoryType, Dictionary<SkillType, SkillAttribute>> _activeSkillsByCategoryDict = new();

        /// <summary>
        /// When the module loads, skills and categories are organized into dictionaries for quick look-ups later on.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public static void CacheData()
        {
            Console.WriteLine("Skill.Cache.CacheData() called - starting skill cache initialization...");
            
            // Cache skill categories
            _categoryCache = _cacheService.BuildEnumCache<SkillCategoryType, SkillCategoryAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", c => c.IsActive)
                .WithFilteredCache("Contributing", c => c.IsActive) // Will be populated by skills
                .Build();

            // Cache skills
            _skillCache = _cacheService.BuildEnumCache<SkillType, SkillAttribute>()
                .WithAllItems()
                .WithFilteredCache("Active", s => s.IsActive)
                .WithFilteredCache("Contributing", s => s.ContributesToSkillCap)
                .WithFilteredCache("Crafting", s => s.IsActive && s.IsShownInCraftMenu)
                .WithFilteredCache("Researchable", s => s.IsActive && s.IsShownInResearchMenu)
                .WithGroupedCache<SkillCategoryType>("ByCategory", s => s.Category)
                .Build();

            // Initialize category lists
            foreach (var category in _categoryCache.AllItems.Keys)
            {
                _allSkillsByCategory[category] = new List<SkillType>();
                if (_categoryCache.AllItems[category].IsActive)
                {
                    _activeSkillsByCategory[category] = new List<SkillType>();
                }
            }

            // Process skills for additional caches
            foreach (var (skillType, skillDetail) in _skillCache.AllItems)
            {
                // Add to the skills by category cache
                _allSkillsByCategory[skillDetail.Category].Add(skillType);

                // Add to active skills by category if both skill and category are active
                if (skillDetail.IsActive && _categoryCache.AllItems[skillDetail.Category].IsActive)
                {
                    if (!_activeSkillsByCategory.ContainsKey(skillDetail.Category))
                        _activeSkillsByCategory[skillDetail.Category] = new List<SkillType>();
                    _activeSkillsByCategory[skillDetail.Category].Add(skillType);
                }
            }

            // Pre-compute caches for fast retrieval
            PopulatePreComputedCaches();

            EventsPlugin.SignalEvent("SWLOR_CACHE_SKILLS_LOADED", GetModule());
            Console.WriteLine($"Loaded {_categoryCache.GetFilteredCache("Active")?.Count ?? 0} skill categories.");
            Console.WriteLine($"Loaded {_skillCache.AllItems.Count} skills.");
            Console.WriteLine("Skill.Cache.CacheData() completed successfully!");
        }

        /// <summary>
        /// Populates pre-computed caches for fast retrieval without LINQ.
        /// </summary>
        private static void PopulatePreComputedCaches()
        {
            if (_skillCache == null)
            {
                Console.WriteLine("ERROR: _skillCache is null in PopulatePreComputedCaches!");
                return;
            }

            Console.WriteLine($"Populating pre-computed caches with {_skillCache.AllItems.Count} skills...");
            
            // Populate skill caches
            foreach (var (skillType, skillAttribute) in _skillCache.AllItems)
            {
                _allSkills[skillType] = skillAttribute;
                
                if (skillAttribute.IsActive)
                {
                    _activeSkills[skillType] = skillAttribute;
                }
                
                if (skillAttribute.ContributesToSkillCap)
                {
                    _contributingSkills[skillType] = skillAttribute;
                    
                    if (skillAttribute.IsActive)
                    {
                        _activeContributingSkills[skillType] = skillAttribute;
                    }
                }
                
                if (skillAttribute.IsActive && skillAttribute.IsShownInCraftMenu)
                {
                    _craftingSkills[skillType] = skillAttribute;
                }
                
                if (skillAttribute.IsActive && skillAttribute.IsShownInResearchMenu)
                {
                    _researchableSkills[skillType] = skillAttribute;
                }
            }

            // Populate category caches
            foreach (var (categoryType, categoryAttribute) in _categoryCache!.AllItems)
            {
                _allCategories[categoryType] = categoryAttribute;
                
                if (categoryAttribute.IsActive)
                {
                    _activeCategories[categoryType] = categoryAttribute;
                }
            }

            // Populate contributing categories
            foreach (var skillAttribute in _contributingSkills.Values)
            {
                var category = skillAttribute.Category;
                if (!_contributingCategories.ContainsKey(category))
                {
                    _contributingCategories[category] = _categoryCache.AllItems[category];
                }
            }

            // Populate active contributing categories
            foreach (var skillAttribute in _activeContributingSkills.Values)
            {
                var category = skillAttribute.Category;
                if (!_activeContributingCategories.ContainsKey(category))
                {
                    _activeContributingCategories[category] = _categoryCache.AllItems[category];
                }
            }

            // Populate skills by category
            foreach (var (skillType, skillAttribute) in _skillCache.AllItems)
            {
                var category = skillAttribute.Category;
                
                if (!_skillsByCategory.ContainsKey(category))
                    _skillsByCategory[category] = new Dictionary<SkillType, SkillAttribute>();
                _skillsByCategory[category][skillType] = skillAttribute;
                
                if (skillAttribute.IsActive)
                {
                    if (!_activeSkillsByCategoryDict.ContainsKey(category))
                        _activeSkillsByCategoryDict[category] = new Dictionary<SkillType, SkillAttribute>();
                    _activeSkillsByCategoryDict[category][skillType] = skillAttribute;
                }
            }
            
            Console.WriteLine($"Pre-computed caches populated successfully. _allSkills.Count: {_allSkills.Count}");
        }

        /// <summary>
        /// Retrieves a list of all skills, including inactive ones.
        /// </summary>
        /// <returns>A list of all skills.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllSkills()
        {
            return _allSkills;
        }

        /// <summary>
        /// Retrieves a list of all skills, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skills.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllActiveSkills()
        {
            return _activeSkills;
        }

        /// <summary>
        /// Retrieves a list of all skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skills contributing towards the skill cap.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllContributingSkills()
        {
            return _contributingSkills;
        }

        /// <summary>
        /// Retrieves a list of active skills which contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of active skills contributing towards the skill cap.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveContributingSkills()
        {
            return _activeContributingSkills;
        }

        /// <summary>
        /// Retrieves a list of all skill categories, including inactive ones.
        /// </summary>
        /// <returns>A list of all skill categories</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllSkillCategories()
        {
            return _allCategories;
        }

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the crafting menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the crafting menu.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveCraftingSkills()
        {
            return _craftingSkills;
        }

        /// <summary>
        /// Retrieves a dictionary of all active skills which are displayed in the research menu.
        /// </summary>
        /// <returns>A dictionary of active skills which are displayed in the research menu.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveResearchableCraftingSkills()
        {
            return _researchableSkills;
        }

        /// <summary>
        /// Retrieves a list of all skill categories, excluding inactive ones.
        /// </summary>
        /// <returns>A list of active skill categories.</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllActiveSkillCategories()
        {
            return _activeCategories;
        }

        /// <summary>
        /// Retrieves a list of all skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categories which have skills that contribute towards the skill cap.</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetAllContributingSkillCategories()
        {
            return _contributingCategories;
        }

        /// <summary>
        /// Retrieves a list of active skill categories which have skills that contribute towards the skill cap.
        /// </summary>
        /// <returns>A list of skill categores which have skills that contribute towards the skill cap.</returns>
        public static Dictionary<SkillCategoryType, SkillCategoryAttribute> GetActiveContributingSkillCategories()
        {
            return _activeContributingCategories;
        }

        /// <summary>
        /// Retrieves all skills by a given category, including inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing skills in the specified category.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetAllSkillsByCategory(SkillCategoryType category)
        {
            return _skillsByCategory.GetValueOrDefault(category) ?? new Dictionary<SkillType, SkillAttribute>();
        }

        /// <summary>
        /// Retrieves active skills by a given category, excluding inactive ones.
        /// </summary>
        /// <param name="category">The category of skills to retrieve.</param>
        /// <returns>A dictionary containing active skills in the specified category.</returns>
        public static Dictionary<SkillType, SkillAttribute> GetActiveSkillsByCategory(SkillCategoryType category)
        {
            return _activeSkillsByCategoryDict.GetValueOrDefault(category) ?? new Dictionary<SkillType, SkillAttribute>();
        }

        /// <summary>
        /// Retrieves details about a specific skill.
        /// </summary>
        /// <param name="skillType">The skill whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill.</returns>
        public static SkillAttribute GetSkillDetails(SkillType skillType)
        {
            if (_allSkills.Count == 0)
            {
                Console.WriteLine($"ERROR: _allSkills is empty when trying to get {skillType}!");
                Console.WriteLine($"_skillCache is null: {_skillCache == null}");
                if (_skillCache != null)
                {
                    Console.WriteLine($"_skillCache.AllItems.Count: {_skillCache.AllItems.Count}");
                }
                
                // Try to populate the cache on-demand as a fallback
                Console.WriteLine("Attempting to populate skill cache on-demand...");
                try
                {
                    CacheData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to populate skill cache on-demand: {ex.Message}");
                }
            }
            
            return _allSkills.TryGetValue(skillType, out var skill) 
                ? skill 
                : throw new KeyNotFoundException($"Skill {skillType} not found in cache");
        }

        /// <summary>
        /// Retrieves details about a specific skill category.
        /// </summary>
        /// <param name="category">The category whose details we will retrieve.</param>
        /// <returns>An object containing details about a skill category.</returns>
        public static SkillCategoryAttribute GetSkillCategoryDetails(SkillCategoryType category)
        {
            return _allCategories.TryGetValue(category, out var categoryDetail) 
                ? categoryDetail 
                : throw new KeyNotFoundException($"Skill category {category} not found in cache");
        }
    }
}

