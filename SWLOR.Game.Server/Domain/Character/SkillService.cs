using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Core;
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
using SkillCategoryType = SWLOR.Game.Server.Domain.Foundation.SkillCategoryType;
using SkillAttribute = SWLOR.Game.Server.Domain.Foundation.SkillAttribute;
using SkillCategoryAttribute = SWLOR.Game.Server.Domain.Foundation.SkillCategoryAttribute;
using SkillType = SWLOR.Game.Server.Domain.Foundation.SkillType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The skill catalog, XP awards, rank rules and decay: the logic and state that used to be static in
    /// the facade <c>Skill</c>, moved here unchanged. The event handlers run on the container's instance, and static
    /// callers reach it through <c>GameServices.Skill</c>.
    /// </summary>
    public class SkillService : ISkillService
    {
        private readonly IDBService _db;
        private readonly ITwinPistolPairQuery _twinPistolEquipment;

        private readonly ICompanionLookup _companions;

        public SkillService(
            IDBService db,
            ITwinPistolPairQuery twinPistolEquipment,
            ICompanionLookup companions)
        {
            _companions = companions;
            _db = db;
            _twinPistolEquipment = twinPistolEquipment;
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

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Adds missing skills in Redis; the login already did.")]
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
        /// Reads whether a skill counts toward the skill cap from the catalog table, without copying it. The rank rules call this in a per-skill loop.
        /// </summary>
        /// <param name="skillType">The skill to look up.</param>
        /// <param name="contributes">Whether the skill counts toward the cap; false when the table has not been built.</param>
        /// <returns>false while the catalog table is empty (before the module cache runs), so the caller uses its own fallback; otherwise true.</returns>
        public bool TryIsContributingSkill(SkillType skillType, out bool contributes)
        {
            if (_allSkillsContributingToCap.Count <= 0)
            {
                contributes = false;
                return false;
            }

            contributes = _allSkillsContributingToCap.ContainsKey(skillType);
            return true;
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
        [BootPhase(BootReload.Rerun)]
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
        [BootPhase(BootReload.Rerun)]
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

        /// <summary>
        /// When the module loads, cache all XP chart data used for quick access. The chart is a constant table
        /// built when <see cref="SkillXPChart"/> loads, so there is nothing left to build here.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
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

        /// <summary>Whether the creature is a droid (the unconverted <c>Droid</c>).</summary>
        protected virtual bool IsDroid(uint creature)
        {
            return _companions.IsDroid(creature);
        }

        /// <summary>The rank a droid's controller item gives in a skill, 0 when it has none (the unconverted <c>Droid</c>).</summary>
        protected virtual int GetDroidSkillRank(uint creature, SkillType skillType)
        {
            return _companions.GetDroidSkillRank(creature, skillType);
        }
    }
}
