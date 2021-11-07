using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CraftViewModel: GuiViewModelBase<CraftViewModel, CraftPayload>
    {
        public const string ViewName = "CraftView";
        public const string SetUpPartialName = "SetUpPartial";
        public const string CraftPartialName = "CraftPartial";
        private const string BlankTexture = "Blank";
        private static GuiColor _green = new GuiColor(0, 255, 0);
        private static GuiColor _red = new GuiColor(255, 0, 0);

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

        public GuiColor StatusColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        private List<string> _components = new List<string>();
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

        protected override void Initialize(CraftPayload initialPayload)
        {
            _components.Clear();

            _recipe = initialPayload.Recipe;
            var detail = Craft.GetRecipe(_recipe);
            var itemName = Cache.GetItemNameByResref(detail.Resref);
            
            SwitchToSetUpMode();
            StatusColor = _green;
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

            _maxCP = (int)(dbPlayer.CP * 0.33f + skill * 0.75f) + primaryModifier * 2 + secondaryModifier;
            _cp = _maxCP;
            
            _maxDurability = levelDetail.Durability;
            _durability = _maxDurability;


            // 10% penalty per level due to the recipe level being higher than skill level.
            var progressModifier = 0f;
            if (_levelDifference < 0)
            {
                var adjustment = _levelDifference * 0.10f;
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

                _components.Clear();
            }
        };

        private bool IsValidEnhancement(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return false;
            }

            return true;
        }

        public Action OnClickEnhancement1() => () =>
        {
            if (string.IsNullOrWhiteSpace(_enhancement1))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, item =>
                {
                    if (!IsValidEnhancement(item))
                        return;

                    _enhancement1 = ObjectPlugin.Serialize(item);
                    Enhancement1Tooltip = GetName(item);
                    Enhancement1Resref = Item.GetIconResref(item);

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
                    _enhancement1 = string.Empty;
                    Enhancement1Resref = BlankTexture;
                    Enhancement1Tooltip = "Select Enhancement #1";

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement2() => () =>
        {
            if (string.IsNullOrWhiteSpace(_enhancement2))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, item =>
                {
                    if (!IsValidEnhancement(item))
                        return;

                    _enhancement2 = ObjectPlugin.Serialize(item);
                    Enhancement2Tooltip = GetName(item);
                    Enhancement2Resref = Item.GetIconResref(item);

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
                    _enhancement2 = string.Empty;
                    Enhancement2Resref = BlankTexture;
                    Enhancement2Tooltip = "Select Enhancement #2";
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
        /// Determines if the player has all of the necessary components for this recipe.
        /// </summary>
        /// <returns>true if player has all components, false otherwise</returns>
        private bool HasAllComponents(List<uint> components)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var remainingComponents = recipe.Components.ToDictionary(x => x.Key, y => y.Value);

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
                    // Split the stack. The copy will remain in the player's inventory.
                    var copy = CopyItem(component, Player, true);
                    SetItemStackSize(copy, quantity - remainingComponents[resref]);
                    ForceRefreshObjectUUID(copy);

                    // The original will be used for crafting.
                    SetItemStackSize(component, remainingComponents[resref]);

                    remainingComponents[resref] = 0;
                    _components.Add(ObjectPlugin.Serialize(component));
                }
                // Player's component stack size is less than or equal to the amount required.
                else if (quantity <= remainingComponents[resref])
                {
                    remainingComponents[resref] -= quantity;
                    _components.Add(ObjectPlugin.Serialize(component));
                }

                if (remainingComponents[resref] <= 0)
                    remainingComponents.Remove(resref);
            }

            var hasAllComponents = remainingComponents.Count <= 0;

            // If we're missing some components, clear the serialized component list.
            if(!hasAllComponents)
                _components.Clear();

            return hasAllComponents;
        }

        private void SwitchToCraftMode()
        {
            StatusText = string.Empty;
            StatusColor = _green;

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
        }

        private void SwitchToSetUpMode()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var detail = Craft.GetRecipe(_recipe);

            IsInCraftMode = false;
            IsInSetupMode = true;
            IsAutoCraftEnabled = dbPlayer.CraftedRecipes.ContainsKey(_recipe);
            IsClosable = true;
            YourSkill = $"Your Skill: {Skill.GetSkillDetails(detail.Skill).Name} {dbPlayer.Skills[detail.Skill].Rank}";

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

            _durability = _maxDurability;
            _progress = 0;
            _quality = 0;
            _cp = _maxCP;

            Enhancement1Resref = BlankTexture;
            Enhancement2Resref = BlankTexture;
            Enhancement1Tooltip = "Select Enhancement #1";
            Enhancement2Tooltip = "Select Enhancement #2";
        }

        public Action OnClickAutoCraft() => () =>
        {

        };

        public Action OnClickManualCraft() => () =>
        {
            var components = GetComponents();
            if (!HasAllComponents(components))
            {
                StatusText = $"Missing components!";
                StatusColor = _red;

                return;
            }

            // The components get serialized during the HasAllComponents call.
            // Since we have everything, go ahead and delete the items from the player's inventory.
            // We're ready to craft!
            foreach (var component in components)
            {
                DestroyObject(component);
            }
            
            SwitchToCraftMode();
        };

        private int CalculateProgress(int baseProgress)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var primaryModifier = GetAbilityModifier(_primaryAbility, Player);
            var secondaryModifier = GetAbilityModifier(_secondaryAbility, Player);
            var control = dbPlayer.Control;
            var progress = baseProgress + (primaryModifier * 2) + secondaryModifier + (int)(control * 0.65f);

            return progress;
        }

        private int CalculateQuality(int baseQuality)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var primaryModifier = GetAbilityModifier(_primaryAbility, Player);
            var secondaryModifier = GetAbilityModifier(_secondaryAbility, Player);
            var craftsmanship = dbPlayer.Craftsmanship;

            var quality = baseQuality + (primaryModifier * 8) + (secondaryModifier * 3) + (int)(craftsmanship * 0.75f);
            return quality;
        }

        private void ProcessSuccess()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var item = CreateItemOnObject(recipe.Resref, Player, recipe.Quantity);

            // todo: apply enhancements

            _components.Clear();
            SwitchToSetUpMode();
            RefreshRecipeStats();
            StatusText = "Successfully created the item!";
            StatusColor = _green;
        }

        private void ProcessFailure()
        {
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

            _components.Clear();

            SwitchToSetUpMode();
            RefreshRecipeStats();
            StatusText = "Failed to craft the item...";
            StatusColor = _red;
        }

        private void DoSynthesis(string abilityName, int baseAmount, int chance)
        {
            _durability -= 10;
            if (_durability < 0)
                _durability = 0;

            if (Random.D100(1) <= chance)
            {
                var progress = CalculateProgress(baseAmount);
                _progress += progress;


                StatusText = $"{abilityName}: Success!";
                StatusColor = _green;
            }
            else
            {
                StatusText = $"{abilityName}: FAILURE";
                StatusColor = _red;
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

        private void DoTouch(string abilityName, int baseAmount, int chance)
        {
            _durability -= 10;
            if (_durability < 0)
                _durability = 0;

            if (Random.D100(1) <= chance)
            {
                var quality = CalculateQuality(baseAmount);
                _quality += quality;

                StatusText = $"{abilityName}: Success!";
                StatusColor = _green;
            }
            else
            {
                StatusText = $"{abilityName}: FAILURE";
                StatusColor = _red;
            }

            if (_durability <= 0)
            {
                ProcessFailure();
            }

            RefreshRecipeStats();
        }

        public Action OnClickBasicSynthesis() => () =>
        {
            DoSynthesis("Basic Synthesis", 10, 90);
        };

        public Action OnClickRapidSynthesis() => () =>
        {
            DoSynthesis("Rapid Synthesis", 30, 75);
        };

        public Action OnClickCarefulSynthesis() => () =>
        {
            DoSynthesis("Careful Synthesis", 80, 50);
        };


        public Action OnClickBasicTouch() => () =>
        {
            DoTouch("Basic Touch", 10, 90);
        };

        public Action OnClickStandardTouch() => () =>
        {
            DoTouch("Standard Touch", 30, 75);
        };

        public Action OnClickPreciseTouch() => () =>
        {
            DoTouch("Precise Touch", 80, 50);
        };


        public Action OnClickMastersMend() => () =>
        {

        };

        public Action OnClickSteadyHand() => () =>
        {

        };

        public Action OnClickMuscleMemory() => () =>
        {

        };

        public Action OnClickVeneration() => () =>
        {

        };

        public Action OnClickWasteNot() => () =>
        {

        };
    }
}
