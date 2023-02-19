using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CraftViewModel: GuiViewModelBase<CraftViewModel, CraftPayload>,
        IGuiRefreshable<SkillXPRefreshEvent>
    {
        public const string ViewName = "CraftView";
        public const string SetUpPartialName = "SetUpPartial";
        public const string CraftPartialName = "CraftPartial";
        private const string BlankTexture = "Blank";

        private RecipeType _recipe;
        
        private PerkType _rapidSynthesisPerk;
        private PerkType _carefulSynthesisPerk;
        
        private PerkType _basicTouchPerk;
        private PerkType _standardTouchPerk;
        private PerkType _preciseTouchPerk;

        private PerkType _mastersMendPerk;
        private PerkType _steadyHandPerk;
        private PerkType _muscleMemoryPerk;

        private PerkType _venerationPerk;
        private PerkType _wasteNotPerk;

        public bool IsClosable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string RecipeName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string RecipeLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string YourSkill
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> RecipeDescription
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RecipeColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public bool IsInSetupMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsInCraftMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        private bool _isInAutoCraftMode;

        public string ControlTotal
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CraftsmanshipTotal
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsEnhancement1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Enhancement1Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement2Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsAutoCraftEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string DurabilityText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float DurabilityPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string ProgressText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float ProgressPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string QualityText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float QualityPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string CP
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsRapidSynthesisEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsCarefulSynthesisEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsBasicTouchEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsStandardTouchEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsPreciseTouchEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsMastersMendEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsSteadyHandEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsMuscleMemoryEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsVenerationEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsWasteNotEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AutoCraft
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor StatusColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        private readonly List<string> _components = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement1 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement2 = new();
        private string _enhancement1;
        private string _enhancement2;
        private int _durability;
        private int _maxDurability;
        private int _progress;
        private int _maxProgress;
        private int _quality;
        private int _maxQuality;
        private int _cp;
        private int _maxCP;
        private int _levelDifference;
        private AbilityType _primaryAbility;
        private AbilityType _secondaryAbility;
        private bool _isSteadyHandActive;
        private bool _isMuscleMemoryActive;
        private int _venerationStepsRemaining;
        private int _wasteNotStepsRemaining;

        protected override void Initialize(CraftPayload initialPayload)
        {
            _components.Clear();

            _recipe = initialPayload.Recipe;
            var detail = Craft.GetRecipe(_recipe);
            var itemName = Cache.GetItemNameByResref(detail.Resref);
            
            SwitchToSetUpMode();
            StatusColor = GuiColor.Green;
            StatusText = string.Empty;

            IsEnhancement1Visible = detail.EnhancementSlots >= 1;
            IsEnhancement2Visible = detail.EnhancementSlots >= 2;

            RecipeName = $"Recipe: {detail.Quantity}x {itemName}";
            RecipeLevel = $"Level: {detail.Level}";
            
            var (recipeDescription, recipeColors) = Craft.BuildRecipeDetail(Player, _recipe);
            RecipeDescription = recipeDescription;
            RecipeColors = recipeColors;

            IsRapidSynthesisEnabled = false;
            IsCarefulSynthesisEnabled = false;
            
            IsBasicTouchEnabled = false;
            IsStandardTouchEnabled = false;
            IsPreciseTouchEnabled = false;
            
            IsMastersMendEnabled = false;
            IsSteadyHandEnabled = false;
            IsMuscleMemoryEnabled = false;

            IsVenerationEnabled = false;
            IsWasteNotEnabled = false;

            LoadRequiredPerks();
            LoadCraftingState();
            RefreshRecipeStats();
        }

        private void LoadRequiredPerks()
        {
            var detail = Craft.GetRecipe(_recipe);
            switch (detail.Skill)
            {
                case SkillType.Smithery:
                    _primaryAbility = AbilityType.Might;
                    _secondaryAbility = AbilityType.Vitality;

                    _rapidSynthesisPerk = PerkType.RapidSynthesisSmithery;
                    _carefulSynthesisPerk = PerkType.CarefulSynthesisSmithery;
                    
                    _basicTouchPerk = PerkType.BasicTouchSmithery;
                    _standardTouchPerk = PerkType.StandardTouchSmithery;
                    _preciseTouchPerk = PerkType.PreciseTouchSmithery;

                    _mastersMendPerk = PerkType.MastersMendSmithery;
                    _steadyHandPerk = PerkType.SteadyHandSmithery;
                    _muscleMemoryPerk = PerkType.MuscleMemorySmithery;

                    _venerationPerk = PerkType.VenerationSmithery;
                    _wasteNotPerk = PerkType.WasteNotSmithery;
                    break;
                case SkillType.Fabrication:
                    _primaryAbility = AbilityType.Perception;
                    _secondaryAbility = AbilityType.Willpower;

                    _rapidSynthesisPerk = PerkType.RapidSynthesisFabrication;
                    _carefulSynthesisPerk = PerkType.CarefulSynthesisFabrication;

                    _basicTouchPerk = PerkType.BasicTouchFabrication;
                    _standardTouchPerk = PerkType.StandardTouchFabrication;
                    _preciseTouchPerk = PerkType.PreciseTouchFabrication;

                    _mastersMendPerk = PerkType.MastersMendFabrication;
                    _steadyHandPerk = PerkType.SteadyHandFabrication;
                    _muscleMemoryPerk = PerkType.MuscleMemoryFabrication;

                    _venerationPerk = PerkType.VenerationFabrication;
                    _wasteNotPerk = PerkType.WasteNotFabrication;
                    break;
                case SkillType.Agriculture:
                    _primaryAbility = AbilityType.Social;
                    _secondaryAbility = AbilityType.Willpower;

                    _rapidSynthesisPerk = PerkType.RapidSynthesisCooking;
                    _carefulSynthesisPerk = PerkType.CarefulSynthesisCooking;

                    _basicTouchPerk = PerkType.BasicTouchCooking;
                    _standardTouchPerk = PerkType.StandardTouchCooking;
                    _preciseTouchPerk = PerkType.PreciseTouchCooking;

                    _mastersMendPerk = PerkType.MastersMendCooking;
                    _steadyHandPerk = PerkType.SteadyHandCooking;
                    _muscleMemoryPerk = PerkType.MuscleMemoryCooking;

                    _venerationPerk = PerkType.VenerationCooking;
                    _wasteNotPerk = PerkType.WasteNotCooking;
                    break;
                case SkillType.Engineering:
                    _primaryAbility = AbilityType.Vitality;
                    _secondaryAbility = AbilityType.Agility;

                    _rapidSynthesisPerk = PerkType.RapidSynthesisEngineering;
                    _carefulSynthesisPerk = PerkType.CarefulSynthesisEngineering;

                    _basicTouchPerk = PerkType.BasicTouchEngineering;
                    _standardTouchPerk = PerkType.StandardTouchEngineering;
                    _preciseTouchPerk = PerkType.PreciseTouchEngineering;

                    _mastersMendPerk = PerkType.MastersMendEngineering;
                    _steadyHandPerk = PerkType.SteadyHandEngineering;
                    _muscleMemoryPerk = PerkType.MuscleMemoryEngineering;

                    _venerationPerk = PerkType.VenerationEngineering;
                    _wasteNotPerk = PerkType.WasteNotEngineering;
                    break;
            }
        }

        private void LoadCraftingState()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipe = Craft.GetRecipe(_recipe);
            var skill = dbPlayer.Skills[recipe.Skill].Rank;
            var levelDetail = Craft.GetRecipeLevelDetail(recipe.Level);
            _levelDifference = skill - recipe.Level;
            
            // 33% CP from equipment and other sources, 75% from skill. +2 per primary modifier. +1 per secondary modifier.
            var primaryModifier = GetAbilityModifier(_primaryAbility, Player);
            var secondaryModifier = GetAbilityModifier(_secondaryAbility, Player);
            if (primaryModifier < 0)
                primaryModifier = 0;
            if (secondaryModifier < 0)
                secondaryModifier = 0;

            var cp = dbPlayer.CPBonus.ContainsKey(recipe.Skill) 
                ? dbPlayer.CPBonus[recipe.Skill] 
                : 0;

            _maxCP = (int)(cp + skill * 0.75f) + primaryModifier * 2 + secondaryModifier;
            _cp = _maxCP;
            
            _maxDurability = levelDetail.Durability;
            _durability = _maxDurability;


            // 25% penalty per level due to the recipe level being higher than skill level.
            var progressModifier = 0f;
            if (_levelDifference < 0)
            {
                var adjustment = _levelDifference * 0.25f;
                if (adjustment > 2.00f)
                    adjustment = 2.00f;
                progressModifier = -adjustment;
            }
            // 5% bonus per level due to the recipe level being lower than skill level.
            else if (_levelDifference > 0)
            {
                var adjustment = _levelDifference * 0.05f;
                if (adjustment > 0.25f)
                    adjustment = 0.25f;
                progressModifier = adjustment;
            }

            _maxProgress = (int)(levelDetail.Progress + levelDetail.Progress * progressModifier);
            _progress = 0;

            _maxQuality = levelDetail.Quality;
            _quality = 0;
        }

        private void RefreshRecipeStats()
        {
            var autoCraftChance = CalculateAutoCraftChance();
            AutoCraft = IsAutoCraftEnabled
                ? $"Auto Craft [{autoCraftChance:F}%]"
                : "Auto Craft";

            CP = $"CP: {_cp}/{_maxCP}";

            DurabilityPercentage = (float)_durability / (float)_maxDurability;
            DurabilityText = $"Durability ({_durability}/{_maxDurability})";

            ProgressPercentage = (float)_progress / (float)_maxProgress;
            ProgressText = $"Progress ({_progress}/{_maxProgress})";

            QualityPercentage = (float)_quality / (float)_maxQuality;
            QualityText = $"Quality ({_quality}/{_maxQuality})";
        }

        public Action OnCloseWindow() => () =>
        {
            // Closing the window while in craft mode results in an immediate failure,
            // possibly resulting in losing components and enhancements
            if (IsInCraftMode)
            {
                ProcessFailure();
            }
            // Closing the window before entering craft mode returns the items to the player.
            else
            {
                if (!string.IsNullOrWhiteSpace(_enhancement1))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement1);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement1 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement2))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement2);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement2 = string.Empty;
                }

                foreach (var serialized in _components)
                {
                    var item = ObjectPlugin.Deserialize(serialized);
                    ObjectPlugin.AcquireItem(Player, item);
                }

                _itemPropertiesEnhancement1.Clear();
                _itemPropertiesEnhancement2.Clear();
                _components.Clear();
            }
        };

        private bool IsValidEnhancement(uint item)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var typeIP = ItemPropertyType.Invalid;

            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return false;
            }

            if (recipe.EnhancementType == RecipeEnhancementType.Armor)
            {
                typeIP = ItemPropertyType.ArmorEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Weapon)
            {
                typeIP = ItemPropertyType.WeaponEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Structure)
            {
                typeIP = ItemPropertyType.StructureEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Food)
            {
                typeIP = ItemPropertyType.FoodEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Starship)
            {
                typeIP = ItemPropertyType.StarshipEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Module)
            {
                typeIP = ItemPropertyType.ModuleEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Droid)
            {
                typeIP = ItemPropertyType.DroidEnhancement;
            }

            if (typeIP == ItemPropertyType.Invalid)
            {
                FloatingTextStringOnCreature("Item must be an enhancement.", Player, false);
                return false;
            }

            var foundIp = false;
            var enhancementLevel = -1;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == typeIP)
                {
                    foundIp = true;
                }

                if (type == ItemPropertyType.EnhancementLevel)
                {
                    enhancementLevel = GetItemPropertyCostTableValue(ip);
                }

                if(foundIp && enhancementLevel > -1)
                    break;
            }

            if (!foundIp || enhancementLevel == -1)
            {
                FloatingTextStringOnCreature("Item must be an enhancement.", Player, false);
                return false;
            }

            var levelDiff = enhancementLevel - recipe.Level;
            if (levelDiff > 5)
            {
                FloatingTextStringOnCreature("Enhancement must be within 5 levels of recipe level.", Player, false);
                return false;
            }

            return true;
        }

        private int CalculateProgressPenaltyAndProcessItemProperties(uint item, List<ItemProperty> itemProperties)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var progressPenalty = 0;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                var subType = GetItemPropertySubType(ip);
                var amount = GetItemPropertyCostTableValue(ip);

                // Progress Penalty - Add to total
                if (type == ItemPropertyType.ProgressPenalty)
                {
                    progressPenalty += GetItemPropertyCostTableValue(ip);
                }
                // Enhancement - Add to item property list.
                else if (type == ItemPropertyType.ArmorEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Armor)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
                else if (type == ItemPropertyType.WeaponEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Weapon)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
                else if (type == ItemPropertyType.StructureEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Structure)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
                else if (type == ItemPropertyType.FoodEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Food)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
                else if (type == ItemPropertyType.StarshipEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Starship)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
                else if (type == ItemPropertyType.ModuleEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Module)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
                else if (type == ItemPropertyType.DroidEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Droid)
                {
                    var itemProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                    itemProperties.Add(itemProperty);
                }
            }

            return progressPenalty;
        }

        public Action OnClickEnhancement1() => () =>
        {
            if (string.IsNullOrWhiteSpace(_enhancement1))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                {
                    if (!IsValidEnhancement(item))
                        return;

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement1);
                    _enhancement1 = ObjectPlugin.Serialize(item);
                    Enhancement1Tooltip = GetName(item);
                    Enhancement1Resref = Item.GetIconResref(item);
                    _maxProgress += progressPenalty;

                    DestroyObject(item);
                    RefreshRecipeStats();
                });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_enhancement1);
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement1);
                    _enhancement1 = string.Empty;
                    Enhancement1Resref = BlankTexture;
                    Enhancement1Tooltip = "Select Enhancement #1";
                    _maxProgress -= progressPenalty;
                    _itemPropertiesEnhancement1.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement2() => () =>
        {
            if (string.IsNullOrWhiteSpace(_enhancement2))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                {
                    if (!IsValidEnhancement(item))
                        return;

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement2);
                    _enhancement2 = ObjectPlugin.Serialize(item);
                    Enhancement2Tooltip = GetName(item);
                    Enhancement2Resref = Item.GetIconResref(item);
                    _maxProgress += progressPenalty;

                    DestroyObject(item);
                    RefreshRecipeStats();
                });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_enhancement2);
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement2);
                    _enhancement2 = string.Empty;
                    Enhancement2Resref = BlankTexture;
                    Enhancement2Tooltip = "Select Enhancement #2";
                    _maxProgress -= progressPenalty;
                    _itemPropertiesEnhancement2.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        private List<uint> GetComponents()
        {
            var components = new List<uint>();
            var recipe = Craft.GetRecipe(_recipe);

            for (var item = GetFirstItemInInventory(Player); GetIsObjectValid(item); item = GetNextItemInInventory(Player))
            {
                var resref = GetResRef(item);
                if(recipe.Components.ContainsKey(resref))
                    components.Add(item);
            }
            
            return components;
        }

        /// <summary>
        /// Determines if the player has all of the necessary components for this recipe
        /// and aggregates them into a new list.
        /// </summary>
        /// <returns>A list of components which will be used, an empty list if not all components are found.</returns>
        private List<uint> AggregateComponents(List<uint> components)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var remainingComponents = recipe.Components.ToDictionary(x => x.Key, y => y.Value);
            var result = new List<uint>();

            for (var index = components.Count - 1; index >= 0; index--)
            {
                var component = components[index];
                var resref = GetResRef(component);

                // Recipe does not need any more of this component type.
                if (!remainingComponents.ContainsKey(resref))
                    continue;

                var quantity = GetItemStackSize(component);

                // Player's component stack size is greater than the amount required.
                if (quantity > remainingComponents[resref])
                {
                    var originalStackSize = GetItemStackSize(component);
                    SetItemStackSize(component, remainingComponents[resref]);
                    _components.Add(ObjectPlugin.Serialize(component));
                    var reducedStackSize = originalStackSize - remainingComponents[resref];
                    SetItemStackSize(component, reducedStackSize);
                    result.Add(component);
                    remainingComponents[resref] = 0;
                }
                // Player's component stack size is less than or equal to the amount required.
                else if (quantity <= remainingComponents[resref])
                {
                    remainingComponents[resref] -= quantity;
                    _components.Add(ObjectPlugin.Serialize(component));
                    result.Add(component);
                    DestroyObject(component);
                }

                if (remainingComponents[resref] <= 0)
                    remainingComponents.Remove(resref);
            }

            var hasAllComponents = remainingComponents.Count <= 0;

            // If we're missing some components, clear the serialized component list and the result list.
            if (!hasAllComponents)
            {
                DelayCommand(0.1f, () =>
                {
                    foreach (var component in _components)
                    {
                        var item = ObjectPlugin.Deserialize(component);
                        ObjectPlugin.AcquireItem(Player, item);
                    }

                    _components.Clear();
                });

                result.Clear();
            }

            return result;
        }

        private void RefreshYourSkill(Player dbPlayer)
        {
            var detail = Craft.GetRecipe(_recipe);
            YourSkill = $"Your Skill: {Skill.GetSkillDetails(detail.Skill).Name} {dbPlayer.Skills[detail.Skill].Rank}";
        }

        private void ApplyImmobility()
        {
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), Player);
        }

        private void RemoveImmobility()
        {
            for (var effect = GetFirstEffect(Player); GetIsEffectValid(effect); effect = GetNextEffect(Player))
            {
                if (GetEffectType(effect) == EffectTypeScript.CutsceneImmobilize)
                {
                    RemoveEffect(Player, effect);
                }
            }
        }

        private void SwitchToSetUpMode()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            IsInCraftMode = false;
            IsInSetupMode = true;
            IsAutoCraftEnabled = dbPlayer.CraftedRecipes.ContainsKey(_recipe);
            IsClosable = true;
            RefreshYourSkill(dbPlayer);

            IsRapidSynthesisEnabled = false;
            IsCarefulSynthesisEnabled = false;

            IsBasicTouchEnabled = false;
            IsStandardTouchEnabled = false;
            IsPreciseTouchEnabled = false;

            IsMastersMendEnabled = false;
            IsSteadyHandEnabled = false;
            IsMuscleMemoryEnabled = false;

            IsVenerationEnabled = false;
            IsWasteNotEnabled = false;

            _isMuscleMemoryActive = false;
            _isSteadyHandActive = false;
            _venerationStepsRemaining = 0;
            _wasteNotStepsRemaining = 0;
            _durability = _maxDurability;
            _progress = 0;
            _quality = 0;
            _cp = _maxCP;

            Enhancement1Resref = BlankTexture;
            Enhancement2Resref = BlankTexture;
            Enhancement1Tooltip = "Select Enhancement #1";
            Enhancement2Tooltip = "Select Enhancement #2";

            RemoveImmobility();
        }

        private void SwitchToCraftMode()
        {
            StatusText = string.Empty;
            StatusColor = GuiColor.Green;

            IsInCraftMode = true;
            IsInSetupMode = false;
            IsAutoCraftEnabled = false;
            IsClosable = false;

            IsRapidSynthesisEnabled = Perk.GetEffectivePerkLevel(Player, _rapidSynthesisPerk) > 0;
            IsCarefulSynthesisEnabled = Perk.GetEffectivePerkLevel(Player, _carefulSynthesisPerk) > 0;

            IsBasicTouchEnabled = Perk.GetEffectivePerkLevel(Player, _basicTouchPerk) > 0;
            IsStandardTouchEnabled = Perk.GetEffectivePerkLevel(Player, _standardTouchPerk) > 0;
            IsPreciseTouchEnabled = Perk.GetEffectivePerkLevel(Player, _preciseTouchPerk) > 0;

            IsMastersMendEnabled = Perk.GetEffectivePerkLevel(Player, _mastersMendPerk) > 0;
            IsSteadyHandEnabled = Perk.GetEffectivePerkLevel(Player, _steadyHandPerk) > 0;
            IsMuscleMemoryEnabled = Perk.GetEffectivePerkLevel(Player, _muscleMemoryPerk) > 0;

            IsVenerationEnabled = Perk.GetEffectivePerkLevel(Player, _venerationPerk) > 0;
            IsWasteNotEnabled = Perk.GetEffectivePerkLevel(Player, _wasteNotPerk) > 0;

            ApplyImmobility();
        }

        private void SwitchToAutoCraftMode()
        {
            StatusText = "Auto-crafting....";
            StatusColor = GuiColor.Cyan;

            IsInCraftMode = false;
            IsInSetupMode = false;
            IsAutoCraftEnabled = false;
            IsClosable = false;
            _isInAutoCraftMode = true;

            ApplyImmobility();
        }

        private float CalculateAutoCraftChance()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipe = Craft.GetRecipe(_recipe);

            const float BaseChance = 65f;
            var craftsmanship = Stat.CalculateCraftsmanship(Player, recipe.Skill);
            var control = Stat.CalculateControl(Player, recipe.Skill);
            var recipeLevel = recipe.Level;
            var levelDiff = dbPlayer.Skills[recipe.Skill].Rank - recipe.Level;
            var difficultyAdjustment = Craft.GetRecipeLevelDetail(recipeLevel).DifficultyAdjustment;

            var craftsmanshipMod = craftsmanship * ((1 - recipeLevel * difficultyAdjustment) * 0.5f);
            var controlMod = control * ((1 - recipeLevel * difficultyAdjustment) * 0.25f);
            var chance = levelDiff * 5 + craftsmanshipMod + controlMod + BaseChance;

            if (chance < 1)
                chance = 1;
            else if (chance > 95)
                chance = 95;

            return chance;
        }

        public Action OnClickAutoCraft() => () =>
        {
            void ProcessAutoCraft()
            {
                _progress += (int)(_maxProgress * 0.1f);
                RefreshRecipeStats();

                if (_progress >= _maxProgress)
                {
                    var chance = CalculateAutoCraftChance();

                    // Auto craft is based solely on the player's calculated craft chance
                    if (Random.NextFloat(1f, 100f) <= chance)
                    {
                        ProcessSuccess();
                        _isInAutoCraftMode = false;
                    }
                    else
                    {
                        ProcessFailure();
                        _isInAutoCraftMode = false;
                    }
                }
                else
                {
                    DelayCommand(1.0f, ProcessAutoCraft);
                }
            }

            if (!string.IsNullOrWhiteSpace(_enhancement1) ||
                !string.IsNullOrWhiteSpace(_enhancement2))
            {
                StatusText = $"Enhancements cannot be installed with auto-craft.";
                StatusColor = GuiColor.Red;
                return;
            }

            if (ProcessComponents())
            {
                SwitchToAutoCraftMode();
                DelayCommand(1.0f, ProcessAutoCraft);
            }
        };

        private bool ProcessComponents()
        {
            var components = GetComponents();
            var aggregateList = AggregateComponents(components);
            if (aggregateList.Count <= 0)
            {
                StatusText = $"Missing components!";
                StatusColor = GuiColor.Red;

                return false;
            }
            
            return true;
        }

        public Action OnClickManualCraft() => () =>
        {
            if (ProcessComponents())
            {
                SwitchToCraftMode();
            }
        };

        private int CalculateProgress(int baseProgress)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipe = Craft.GetRecipe(_recipe);
            var primaryModifier = GetAbilityModifier(_primaryAbility, Player);
            var secondaryModifier = GetAbilityModifier(_secondaryAbility, Player);
            var craftsmanship = Stat.CalculateCraftsmanship(Player, recipe.Skill);
            var delta = dbPlayer.Skills[recipe.Skill].Rank - recipe.Level;
            var recipeDiff = 1 + 0.05f * delta;
            var progress = (int)((baseProgress + primaryModifier * 1.25f + secondaryModifier * 0.75f + craftsmanship * 0.65f) * recipeDiff);

            return progress;
        }

        private int CalculateQuality(int baseQuality)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipe = Craft.GetRecipe(_recipe);
            var primaryModifier = GetAbilityModifier(_primaryAbility, Player);
            var secondaryModifier = GetAbilityModifier(_secondaryAbility, Player);
            var control = Stat.CalculateControl(Player, recipe.Skill);
            var delta = dbPlayer.Skills[recipe.Skill].Rank - recipe.Level;
            var recipeDiff = delta < 0 
                ? 1 + 0.05f * delta 
                : 1;

            var quality = (int)((baseQuality + primaryModifier * 8 + secondaryModifier * 3 + control * 0.75f) * recipeDiff);
            return quality;
        }

        private int CalculateXP(int recipeLevel, int playerLevel, bool firstTime, float qualityPercent)
        {
            var delta = recipeLevel - playerLevel;
            var xp = Skill.GetDeltaXP(delta);
            // 20% bonus for the first time.
            if (firstTime)
                xp += (int)(xp * 0.20f);
            xp += (int)(xp * qualityPercent);

            return xp;
        }

        private void ApplyProperty(uint item, ItemProperty ip)
        {
            var type = GetItemPropertyType(ip);
            var subType = GetItemPropertySubType(ip);
            var amount = GetItemPropertyCostTableValue(ip);
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == type &&
                    (GetItemPropertySubType(property) == -1 || GetItemPropertySubType(property) == subType))
                {
                    amount += GetItemPropertyCostTableValue(property);
                    RemoveItemProperty(item, property);
                }
            }

            var unpacked = ItemPropertyPlugin.UnpackIP(ip);
            unpacked.CostTableValue = amount;
            ip = ItemPropertyPlugin.PackIP(unpacked);
            
            BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
        }

        private void ProcessSuccess()
        {
            // Guard against the client queuing up numerous craft requests which results in duplicate items being spawned.
            if (!IsInCraftMode && !_isInAutoCraftMode)
                return;

            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipe = Craft.GetRecipe(_recipe);
            var item = CreateItemOnObject(recipe.Resref, Player, recipe.Quantity);
            var firstTime = !dbPlayer.CraftedRecipes.ContainsKey(_recipe);
            var propertyTransferChance = (int)(((float)_quality / (float)_maxQuality) * 100);
            var qualityPercent = (float)_quality / (float)_maxQuality; 

            // We override the additional gold price field so that players are encouraged to sell their goods to other players rather than selling to an NPC.
            ItemPlugin.SetAddGoldPieceValue(item, (int) (30 * ((recipe.Level / 10) + 1) + 3.5f * recipe.Level));

            // Apply item properties provided by enhancements, provided the transfer check passes.
            var allProperties = _itemPropertiesEnhancement1.Concat(_itemPropertiesEnhancement2);
            foreach (var ip in allProperties)
            {
                if (Random.D100(1) <= propertyTransferChance)
                {
                    ApplyProperty(item, ip);
                    SendMessageToPC(Player, ColorToken.Green("Enhancement applied successfully."));
                }
                else
                {
                    SendMessageToPC(Player, ColorToken.Red("Enhancement failed to apply."));
                }
            }

            // Food items have increased duration based on quality percentage
            if (recipe.Category == RecipeCategoryType.Food && (int)qualityPercent > 0)
            {
                var durationBonus = (int)qualityPercent;
                var ip = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Duration, durationBonus);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
            }

            // Add the recipe to the completed list (unlocks auto-crafting)
            if (firstTime)
            {
                dbPlayer.CraftedRecipes[_recipe] = DateTime.UtcNow;
                DB.Set(dbPlayer);
            }

            // Give XP plus a percent bonus based on the quality achieved.
            var xp = CalculateXP(recipe.Level, dbPlayer.Skills[recipe.Skill].Rank, firstTime, qualityPercent);
            Skill.GiveSkillXP(Player, recipe.Skill, xp, false, false);

            // Clean up and return to the Set Up mode.
            _itemPropertiesEnhancement1.Clear();
            _itemPropertiesEnhancement2.Clear();
            _enhancement1 = string.Empty;
            _enhancement2 = string.Empty;
            _components.Clear();
            SwitchToSetUpMode();
            LoadCraftingState();
            RefreshRecipeStats();
            StatusText = "Successfully created the item!";
            StatusColor = GuiColor.Green;

            Log.Write(LogGroup.Crafting, $"{GetName(Player)} ({GetObjectUUID(Player)}) successfully crafted '{GetName(item)}'.");
        }

        private void ProcessFailure()
        {
            // Guard against the client queuing up numerous craft requests which results in duplicate items being spawned.
            if (!IsInCraftMode)
                return;

            var recipe = Craft.GetRecipe(_recipe);
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            const int ChanceToLoseItem = 65;

            // Process enhancements
            if (!string.IsNullOrWhiteSpace(_enhancement1) && Random.D100(1) > ChanceToLoseItem)
            {
                var item = ObjectPlugin.Deserialize(_enhancement1);
                ObjectPlugin.AcquireItem(Player, item);
            }
            _enhancement1 = string.Empty;

            if (!string.IsNullOrWhiteSpace(_enhancement2) && Random.D100(1) > ChanceToLoseItem)
            {
                var item = ObjectPlugin.Deserialize(_enhancement2);
                ObjectPlugin.AcquireItem(Player, item);
            }
            _enhancement2 = string.Empty;

            // Process components
            foreach (var serialized in _components)
            {
                if (Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(serialized);
                    ObjectPlugin.AcquireItem(Player, item);
                }
            }

            _itemPropertiesEnhancement1.Clear();
            _itemPropertiesEnhancement2.Clear();
            _components.Clear();

            SwitchToSetUpMode();
            LoadCraftingState();
            RefreshRecipeStats();
            StatusText = "Failed to craft the item...";
            StatusColor = GuiColor.Red;

            // 15% of XP is gained for failures.
            var xp = CalculateXP(recipe.Level, dbPlayer.Skills[recipe.Skill].Rank, false, 0f);
            xp = (int)(xp * 0.15f);
            Skill.GiveSkillXP(Player, recipe.Skill, xp, false, false);

            Log.Write(LogGroup.Crafting, $"{GetName(Player)} ({GetObjectUUID(Player)}) failed to craft '{_recipe}'.");
        }

        private void HandleAction(
            string abilityName, 
            int chance, 
            int cpCost, 
            int durabilityLoss,
            Action successAction)
        {
            if (_cp < cpCost)
            {
                StatusText = "Not enough CP!";
                StatusColor = GuiColor.Red;
                return;
            }

            if (durabilityLoss > 0)
            {
                if (_wasteNotStepsRemaining > 0)
                {
                    _wasteNotStepsRemaining--;
                    durabilityLoss /= 2;
                }

                _durability -= durabilityLoss;
                if (_durability < 0)
                    _durability = 0;
            }

            _cp -= cpCost;

            if (Random.D100(1) <= chance)
            {
                successAction();

                StatusText = $"{abilityName}: Success!";
                StatusColor = GuiColor.Green;
            }
            else
            {
                StatusText = $"{abilityName}: FAILURE";
                StatusColor = GuiColor.Red;
            }

            if (_progress >= _maxProgress)
            {
                _progress = _maxProgress;
                ProcessSuccess();
            }
            else if (_durability <= 0)
            {
                ProcessFailure();
            }

            RefreshRecipeStats();
        }

        public Action OnClickBasicSynthesis() => () =>
        {
            var chance = _isSteadyHandActive ? 100 : 90;

            HandleAction("Basic Synthesis", chance, 0, 10, () =>
            {
                var progress = CalculateProgress(10);
                _progress += progress;
                if (_progress > _maxProgress)
                    _progress = _maxProgress;
                _isSteadyHandActive = false;
            });
        };

        public Action OnClickRapidSynthesis() => () =>
        {
            var chance = _isSteadyHandActive ? 100 : 75;
            var cpCost = 6;
            if (_venerationStepsRemaining > 0)
            {
                _venerationStepsRemaining--;
                cpCost /= 2;
            }

            HandleAction("Rapid Synthesis", chance, cpCost, 10, () =>
            {
                var progress = CalculateProgress(30);
                _progress += progress;
                if (_progress > _maxProgress)
                    _progress = _maxProgress;
                _isSteadyHandActive = false;
            });
        };

        public Action OnClickCarefulSynthesis() => () =>
        {
            var chance = _isSteadyHandActive ? 100 : 50;
            var cpCost = 10;
            if (_venerationStepsRemaining > 0)
            {
                _venerationStepsRemaining--;
                cpCost /= 2;
            }

            HandleAction("Careful Synthesis", chance, cpCost, 10, () =>
            {
                var progress = CalculateProgress(80);
                _progress += progress;
                if (_progress > _maxProgress)
                    _progress = _maxProgress;
                _isSteadyHandActive = false;
            });
        };


        public Action OnClickBasicTouch() => () =>
        {
            var chance = _isMuscleMemoryActive ? 100 : 90;

            HandleAction("Basic Touch", chance, 3, 10, () =>
            {
                var quality = CalculateQuality(10);
                _quality += quality;
                if (_quality > _maxQuality)
                    _quality = _maxQuality;
                _isMuscleMemoryActive = false;
            });
        };

        public Action OnClickStandardTouch() => () =>
        {
            var chance = _isMuscleMemoryActive ? 100 : 75;

            HandleAction("Standard Touch", chance, 6, 10, () =>
            {
                var quality = CalculateQuality(30);
                _quality += quality;
                if (_quality > _maxQuality)
                    _quality = _maxQuality;
                _isMuscleMemoryActive = false;
            });
        };

        public Action OnClickPreciseTouch() => () =>
        {
            var chance = _isMuscleMemoryActive ? 100 : 50;

            HandleAction("Precise Touch",  chance, 10, 10, () =>
            {
                var quality = CalculateQuality(80);
                _quality += quality;
                if (_quality > _maxQuality)
                    _quality = _maxQuality;
                _isMuscleMemoryActive = false;
            });
        };

        public Action OnClickMastersMend() => () =>
        {
            HandleAction("Master's Mend", 100, 10, 0, () =>
            {
                _durability += 30;
                if (_durability > _maxDurability)
                    _durability = _maxDurability;
            });
        };

        public Action OnClickSteadyHand() => () =>
        {
            HandleAction("Steady Hand", 100, 12, 0, () =>
            {
                _isSteadyHandActive = true;
            });
        };

        public Action OnClickMuscleMemory() => () =>
        {
            HandleAction("Muscle Memory", 100, 12, 0, () =>
            {
                _isMuscleMemoryActive = true;
            });
        };

        public Action OnClickVeneration() => () =>
        {
            HandleAction("Veneration", 100, 8, 10, () =>
            {
                _venerationStepsRemaining = 4;
            });
        };

        public Action OnClickWasteNot() => () =>
        {
            HandleAction("Waste Not", 100, 4, 0, () =>
            {
                _wasteNotStepsRemaining = 4;
            });
        };

        public void Refresh(SkillXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            RefreshYourSkill(dbPlayer);
        }
    }
}
