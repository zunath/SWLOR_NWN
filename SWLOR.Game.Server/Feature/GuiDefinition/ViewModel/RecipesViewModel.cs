﻿using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RecipesViewModel: GuiViewModelBase<RecipesViewModel, RecipesPayload>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>,
        IGuiRefreshable<PerkRefundedRefreshEvent>
    {
        private int _currentRecipeIndex;
        private readonly List<RecipeType> _recipeTypes = new();
        private const int RecordsPerPage = 20;
        private bool _skipPaginationSearch;
        private SkillType _craftingFilter;
        private uint _selectedBlueprintItem;
        private RecipesUIMode _mode;

        public string Title
        {
            get => Get<string>();
            set => Set(value);
        }
        
        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> Skills
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (!_skipPaginationSearch)
                    Search();
            }
        }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedSkillId
        {
            get => Get<int>();
            set
            {
                Set(value);
                IsSkillSelected = value != 0;
                LoadCategories();

                if (value == 0)
                    SelectedCategoryId = 0;

                if(!_skipPaginationSearch)
                    Search();
            }
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);
                _currentRecipeIndex = -1;

                if (!_skipPaginationSearch)
                    Search();
            }
        }

        public bool IsSkillSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> RecipeNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RecipeColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<bool> RecipeToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> Categories
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
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

        public string RecipeEnhancementSlots
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CanCraftOrResearchRecipe
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> RecipeDetails
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RecipeDetailColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public bool IsSkillEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool ShowSelectBlueprint
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        public bool IsCraftOrResearchVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsInResearchMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ActionButtonText
        {
            get => Get<string>();
            set => Set(value);
        }
        protected override void Initialize(RecipesPayload initialPayload)
        {
            _mode = initialPayload?.Mode ?? RecipesUIMode.Recipes;
            _skipPaginationSearch = true;

            _selectedBlueprintItem = OBJECT_INVALID;
            _craftingFilter = initialPayload?.Skill ?? SkillType.Invalid;
            
            ShowSelectBlueprint = _mode == RecipesUIMode.Crafting || _mode == RecipesUIMode.Research;
            IsSkillEnabled = _craftingFilter == SkillType.Invalid;

            if (_mode == RecipesUIMode.Crafting)
            {
                ActionButtonText = "Craft Item";
                IsCraftOrResearchVisible = _craftingFilter != SkillType.Invalid;
            }
            else if (_mode == RecipesUIMode.Research)
            {
                ActionButtonText = "Research";
                IsCraftOrResearchVisible = true;
            }
            else
            {
                IsCraftOrResearchVisible = false;
            }

            RecipeName = string.Empty;
            RecipeLevel = string.Empty;
            RecipeEnhancementSlots = string.Empty;
            SearchText = string.Empty;
            SelectedPageIndex = 0;
            SelectedSkillId = (int)_craftingFilter;
            SelectedCategoryId = 0;
            _currentRecipeIndex = -1;

            LoadSkills();
            LoadCategories();
            Search();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedSkillId);
            WatchOnClient(model => model.SelectedCategoryId);
            WatchOnClient(model => model.SelectedPageIndex);
            _skipPaginationSearch = false;
        }

        private void LoadSkills()
        {
            var skills = new GuiBindingList<GuiComboEntry>();
            skills.Add(new GuiComboEntry("<All Skills>", 0));

            var set = _mode == RecipesUIMode.Research 
                ? Skill.GetActiveResearchableCraftingSkills() 
                : Skill.GetActiveCraftingSkills();

            foreach (var (type, detail) in set)
            {
                skills.Add(new GuiComboEntry(detail.Name, (int)type));
            }

            Skills = skills;
        }

        private void LoadCategories()
        {
            if (SelectedSkillId == 0 && Categories != null)
            {
                Categories.Clear();
                Categories.Add(new GuiComboEntry("Select...", 0));
                return;
            }

            var selectedSkill = (SkillType)SelectedSkillId;
            var categories = new GuiBindingList<GuiComboEntry>();

            categories.Add(new GuiComboEntry("Select...", 0));
            foreach (var (type, detail) in Craft.GetRecipeCategoriesBySkill(selectedSkill))
            {
                categories.Add(new GuiComboEntry(detail.Name, (int)type));
            }

            Categories = categories;
        }

        private void Search()
        {
            Dictionary<RecipeType, RecipeDetail> recipes;

            // Skill and Category selected
            if (SelectedSkillId > 0 && SelectedCategoryId > 0)
            {
                var skill = (SkillType)SelectedSkillId;
                var category = (RecipeCategoryType)SelectedCategoryId;
                if (_mode == RecipesUIMode.Research)
                {
                    recipes = Craft.GetResearchableRecipesBySkillAndCategory(skill, category);
                }
                else
                {
                    recipes = Craft.GetRecipesBySkillAndCategory(skill, category);
                }
            }
            // Only skill selected
            else if (SelectedSkillId > 0)
            {
                var skill = (SkillType)SelectedSkillId;

                if (_mode == RecipesUIMode.Research)
                {
                    recipes = Craft.GetAllResearchableRecipesBySkill(skill);
                }
                else
                {
                    recipes = Craft.GetAllRecipesBySkill(skill);
                }
            }
            // Neither filters selected
            else
            {
                if (_mode == RecipesUIMode.Research)
                {
                    recipes = Craft.GetAllResearchableRecipes();
                }
                else
                {
                    recipes = Craft.GetAllRecipes();
                }
            }

            // Search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                recipes = recipes
                    .Where(x =>
                        Cache.GetItemNameByResref(x.Value.Resref)
                            .ToLower()
                            .Contains(SearchText.ToLower()))
                    .ToDictionary(x => x.Key, y => y.Value);
            }
            
            UpdatePagination(recipes.Count);

            recipes = recipes
                .Skip(SelectedPageIndex * RecordsPerPage)
                .Take(RecordsPerPage)
                .ToDictionary(x => x.Key, y => y.Value);

            var recipeNames = new GuiBindingList<string>();
            var recipeColors = new GuiBindingList<GuiColor>();
            var recipeToggles = new GuiBindingList<bool>();
            _recipeTypes.Clear();

            foreach (var (type, detail) in recipes)
            {
                var canCraft = _mode == RecipesUIMode.Research
                    ? Craft.CanPlayerResearchRecipe(Player, type)
                    : Craft.CanPlayerCraftRecipe(Player, type);
                var name = $"{Cache.GetItemNameByResref(detail.Resref)} [Lvl. {detail.Level}]";

                recipeNames.Add(name);
                recipeColors.Add(canCraft ? GuiColor.Green : GuiColor.Red);
                recipeToggles.Add(false);
                _recipeTypes.Add(type);
            }

            RecipeNames = recipeNames;
            RecipeColors = recipeColors;
            RecipeToggles = recipeToggles;

            LoadRecipeDetail();
        }

        private void UpdatePagination(int totalRecordCount)
        {
            _skipPaginationSearch = true;
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / RecordsPerPage + (totalRecordCount % RecordsPerPage == 0 ? 0 : 1));

            // Always add page 1. In the event no recipes are found,
            // it still needs to be displayed.
            pageNumbers.Add(new GuiComboEntry($"Page 1", 0));
            for (var x = 2; x <= pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x - 1));
            }

            PageNumbers = pageNumbers;

            // In the event no results are found, default the index to zero
            if (pages <= 0)
                SelectedPageIndex = 0;
            // Otherwise, if current page is outside the new page bounds,
            // set it to the last page in the list.
            else if (SelectedPageIndex > pages - 1)
                SelectedPageIndex = pages - 1;

            _skipPaginationSearch = false;

            _currentRecipeIndex = -1;
            LoadRecipeDetail();
        }
        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickSearch() => Search;


        public Action OnClickPreviousPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            _currentRecipeIndex = -1;
            SelectedPageIndex = newPage;
            Search();
            _skipPaginationSearch = false;
        };

        public Action OnClickNextPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            _currentRecipeIndex = -1;
            SelectedPageIndex = newPage;
            Search();
            _skipPaginationSearch = false;
        };

        public Action OnSelectRecipe() => () =>
        {
            _selectedBlueprintItem = OBJECT_INVALID;

            // Deselect the current recipe.
            if (_currentRecipeIndex > -1)
            {
                RecipeToggles[_currentRecipeIndex] = false;
            }

            _currentRecipeIndex = NuiGetEventArrayIndex();
            RecipeToggles[_currentRecipeIndex] = true;
            LoadRecipeDetail();
        };

        public Action OnClickCraftOrResearch() => () =>
        {
            if (_mode == RecipesUIMode.Crafting)
            {
                if (Gui.IsWindowOpen(Player, GuiWindowType.Craft))
                    return;

                var blueprint = Craft.GetBlueprintDetails(_selectedBlueprintItem);
                if (blueprint.Recipe == RecipeType.Invalid && _currentRecipeIndex < 0)
                    return;

                var recipe = blueprint.Recipe != RecipeType.Invalid
                    ? blueprint.Recipe
                    : _recipeTypes[_currentRecipeIndex];
                var payload = new CraftPayload(recipe, _selectedBlueprintItem);
                Gui.TogglePlayerWindow(Player, GuiWindowType.Craft, payload, TetherObject);
            }
            else if (_mode == RecipesUIMode.Research)
            {
                if (_currentRecipeIndex <= -1)
                    return;

                var recipeType = _recipeTypes[_currentRecipeIndex];
                var recipe = Craft.GetRecipe(recipeType);

                if (recipe.EnhancementType != RecipeEnhancementType.Weapon &&
                    recipe.EnhancementType != RecipeEnhancementType.Armor &&
                    recipe.EnhancementType != RecipeEnhancementType.Food)
                {
                    SendMessageToPC(Player, $"Cannot research that recipe type.");
                    return;
                }

                var propertyId = Property.GetPropertyId(TetherObject);
                var payload = new ResearchPayload(propertyId, OBJECT_INVALID, recipeType);
                Gui.TogglePlayerWindow(Player, GuiWindowType.Research, payload, TetherObject);
            }
        };

        private bool ValidateBlueprint(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("The blueprint must be in your inventory.", Player, false);
                return false;
            }

            var blueprint = Craft.GetBlueprintDetails(item);
            if (blueprint.Recipe == RecipeType.Invalid)
            {
                FloatingTextStringOnCreature("Only blueprints may be selected.", Player, false);
                return false;
            }

            if (blueprint.LicensedRuns <= 0)
            {
                FloatingTextStringOnCreature("No licensed runs remaining on this blueprint.", Player, false);
                return false;
            }

            var recipe = Craft.GetRecipe(blueprint.Recipe);

            if (_mode == RecipesUIMode.Crafting)
            {
                if (recipe.Skill != _craftingFilter)
                {
                    FloatingTextStringOnCreature("Blueprint cannot be crafted with this device.", Player, false);
                    return false;
                }
            }
            else if (_mode == RecipesUIMode.Research)
            {
                var researchLevel = Perk.GetPerkLevel(Player, PerkType.Research);
                var requiredLevel = recipe.Level / 10 + 1;
                if (requiredLevel > 5)
                    requiredLevel = 5;

                if (researchLevel < requiredLevel)
                {
                    FloatingTextStringOnCreature($"Research level {requiredLevel} required.", Player, false);
                    return false;
                }

                if (blueprint.Level >= Craft.MaxResearchLevel)
                {
                    FloatingTextStringOnCreature($"Blueprint cannot be researched any more.", Player, false);
                    return false;
                }
            }

            return true;
        }
        
        public Action OnClickSelectBlueprint() => () =>
        {
            if (_mode == RecipesUIMode.Crafting)
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select the blueprint item you wish to create.", item =>
                {
                    if (!ValidateBlueprint(item))
                        return;

                    var blueprint = Craft.GetBlueprintDetails(item);
                    _selectedBlueprintItem = item;

                    if (_currentRecipeIndex > -1)
                        RecipeToggles[_currentRecipeIndex] = false;
                    DisplayRecipeDetail(blueprint.Recipe, blueprint);

                    CanCraftOrResearchRecipe = Craft.CanPlayerCraftRecipe(Player, blueprint.Recipe);
                });
            }
            else if (_mode == RecipesUIMode.Research)
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select the blueprint you wish to research.", item =>
                {
                    if (!ValidateBlueprint(item))
                        return;
                    
                    Gui.CloseWindow(Player, GuiWindowType.Recipes, Player);

                    var blueprint = Craft.GetBlueprintDetails(item);
                    var propertyId = Property.GetPropertyId(TetherObject);
                    var payload = new ResearchPayload(propertyId, item, blueprint.Recipe);
                    Gui.TogglePlayerWindow(Player, GuiWindowType.Research, payload, TetherObject);
                });
            }
        };

        private void DisplayRecipeDetail(RecipeType recipe, BlueprintDetail blueprint)
        {
            var detail = Craft.GetRecipe(recipe);
            var itemName = Cache.GetItemNameByResref(detail.Resref);
            var enhancementSlotType = "N/A";

            if (detail.EnhancementType == RecipeEnhancementType.Weapon)
                enhancementSlotType = "Weapon";
            else if (detail.EnhancementType == RecipeEnhancementType.Armor)
                enhancementSlotType = "Armor";
            else if (detail.EnhancementType == RecipeEnhancementType.Structure)
                enhancementSlotType = "Structure";
            else if (detail.EnhancementType == RecipeEnhancementType.Food)
                enhancementSlotType = "Food";
            
            RecipeName = $"Recipe: {detail.Quantity}x {itemName}";
            RecipeLevel = $"Level: {detail.Level}";
            RecipeEnhancementSlots = $"Enhancement Slots: {detail.EnhancementSlots}x {enhancementSlotType}";
            var (recipeDetails, recipeDetailColors) = Craft.BuildRecipeDetail(Player, recipe, blueprint);

            RecipeDetails = recipeDetails;
            RecipeDetailColors = recipeDetailColors;
            
            if(_mode == RecipesUIMode.Crafting)
                CanCraftOrResearchRecipe = Craft.CanPlayerCraftRecipe(Player, recipe);
            else if (_mode == RecipesUIMode.Research)
                CanCraftOrResearchRecipe = Craft.CanPlayerResearchRecipe(Player, recipe);
        }

        private void ClearRecipeDetail()
        {
            RecipeName = string.Empty;
            RecipeLevel = string.Empty;
            RecipeEnhancementSlots = string.Empty;
            RecipeDetails = new GuiBindingList<string>();
            RecipeDetailColors = new GuiBindingList<GuiColor>();
            _currentRecipeIndex = -1;
            CanCraftOrResearchRecipe = false;
        }
        
        private void LoadRecipeDetail()
        {
            if (_currentRecipeIndex > -1)
            {
                var selectedRecipe = _recipeTypes[_currentRecipeIndex];
                DisplayRecipeDetail(selectedRecipe, null);
            }
            else
            {
                ClearRecipeDetail();
            }
        }

        public void Refresh(PerkAcquiredRefreshEvent payload)
        {
            Search();
        }

        public void Refresh(PerkRefundedRefreshEvent payload)
        {
            Search();
        }
    }
}
