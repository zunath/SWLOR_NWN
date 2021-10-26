using System;
using System.Collections.Generic;
using System.Diagnostics;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class RecipesViewModel: GuiViewModelBase<RecipesViewModel, GuiPayloadBase>
    {
        private int _currentRecipeIndex;

        private readonly List<RecipeType> _recipeTypes = new List<RecipeType>();

        public int SelectedSkillId
        {
            get => Get<int>();
            set
            {
                Set(value);
                SelectedCategoryId = 0;
                IsSkillSelected = value != 0;
                _currentRecipeIndex = -1;
                IsRecipeSelected = false;
                LoadCategories();
            }
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);
                _currentRecipeIndex = -1;
                IsRecipeSelected = false;
                LoadRecipes();
            }
        }

        public bool ShowAll
        {
            get => Get<bool>();
            set
            {
                Set(value);
                _currentRecipeIndex = -1;
                IsRecipeSelected = false;
                LoadRecipes();
            }
        }

        public bool IsSkillSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsRecipeSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> Recipes
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> Colors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<bool> Selections
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

        public string RecipeModSlots
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> RecipeRequirements
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RecipeRequirementColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> RecipeComponents
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RecipeComponentColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public RecipesViewModel()
        {
            Recipes = new GuiBindingList<string>();
            Colors = new GuiBindingList<GuiColor>();
            Selections = new GuiBindingList<bool>();
            Categories = new GuiBindingList<GuiComboEntry>();
            RecipeRequirements = new GuiBindingList<string>();
            RecipeRequirementColors = new GuiBindingList<GuiColor>();
            RecipeComponents = new GuiBindingList<string>();
            RecipeComponentColors = new GuiBindingList<GuiColor>();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedSkillId = 0;
            SelectedCategoryId = 0;
            _currentRecipeIndex = -1;
            ShowAll = false;
            LoadCategories();
            LoadRecipes();

            WatchOnClient(model => model.SelectedSkillId);
            WatchOnClient(model => model.SelectedCategoryId);
            WatchOnClient(model => model.ShowAll);
        }

        private void LoadCategories()
        {
            if (SelectedSkillId == 0)
            {
                Categories.Clear();
                Categories.Add(new GuiComboEntry("Select...", 0));
                return;
            }

            var selectedSkill = (SkillType) SelectedSkillId;
            var categories = new GuiBindingList<GuiComboEntry>();

            categories.Add(new GuiComboEntry("Select...", 0));
            foreach (var (type, detail) in Craft.GetRecipeCategoriesBySkill(selectedSkill))
            {
                categories.Add(new GuiComboEntry(detail.Name, (int)type));
            }

            Categories = categories;
        }

        private void LoadRecipes()
        {
            if (SelectedSkillId == 0 || SelectedCategoryId == 0)
            {
                _currentRecipeIndex = -1;
                _recipeTypes.Clear();
                Recipes.Clear();
                Colors.Clear();
                Selections.Clear();
                IsRecipeSelected = false;
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            var recipes = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var selections = new GuiBindingList<bool>();
            var skillType = (SkillType) SelectedSkillId;
            var categoryType = (RecipeCategoryType) SelectedCategoryId;

            foreach (var (type, detail) in Craft.GetRecipesBySkillAndCategory(skillType, categoryType))
            {
                var canCraft = CanPlayerCraftRecipe(type);

                // Show All is unchecked and player can't craft this. Skip it.
                if (!ShowAll && !canCraft)
                    continue;
                
                var itemName = Cache.GetItemNameByResref(detail.Resref);
                var name = $"{detail.Quantity}x {itemName}";
                var color = canCraft 
                    ? new GuiColor(0, 255, 0)
                    : new GuiColor(255, 0, 0);

                recipes.Add(name);
                colors.Add(color);
                selections.Add(false);
                _recipeTypes.Add(type);
            }

            Recipes = recipes;
            Colors = colors;
            Selections = selections;

            sw.Stop();
            Console.WriteLine($"LoadRecipes: {sw.ElapsedMilliseconds}ms");
        }

        private bool CanPlayerCraftRecipe(RecipeType recipeType)
        {
            var recipe = Craft.GetRecipe(recipeType);
            if (recipe.Requirements.Count <= 0) return true;

            foreach (var requirement in recipe.Requirements)
            {
                if (!string.IsNullOrWhiteSpace(requirement.CheckRequirements(Player)))
                {
                    return false;
                }
            }

            return true;
        }

        public Action OnSelectRecipe() => () =>
        {
            // Deselect the current recipe.
            if (_currentRecipeIndex > -1)
            {
                Selections[_currentRecipeIndex] = false;
            }

            _currentRecipeIndex = NuiGetEventArrayIndex();
            IsRecipeSelected = _currentRecipeIndex > -1;
            Selections[_currentRecipeIndex] = true;
            LoadRecipeDetail();
        };

        private void LoadRecipeDetail()
        {
            var selectedRecipe = _recipeTypes[_currentRecipeIndex];
            var detail = Craft.GetRecipe(selectedRecipe);
            var itemName = Cache.GetItemNameByResref(detail.Resref);
            var modSlotType = "N/A";

            if (detail.ModType == RecipeModType.Weapon)
                modSlotType = "Weapon";
            else if (detail.ModType == RecipeModType.Armor)
                modSlotType = "Armor";
            else if (detail.ModType == RecipeModType.Furniture)
                modSlotType = "Furniture";

            RecipeName = $"Recipe: {detail.Quantity}x {itemName}";
            RecipeLevel = $"Level: {detail.Level}";
            RecipeModSlots = $"Mod Slots: {detail.ModSlots}x {modSlotType}";

            var recipeRequirements = new GuiBindingList<string>();
            var recipeRequirementColors = new GuiBindingList<GuiColor>();
            var recipeComponents = new GuiBindingList<string>();
            var recipeComponentColors = new GuiBindingList<GuiColor>();

            recipeRequirements.Add("<---REQUIREMENTS--->");
            recipeRequirementColors.Add(new GuiColor(0, 255,255));
            foreach (var req in detail.Requirements)
            {
                recipeRequirements.Add(req.RequirementText);
                recipeRequirementColors.Add(string.IsNullOrWhiteSpace(req.CheckRequirements(Player))
                    ? new GuiColor(0, 255, 0)
                    : new GuiColor(255, 0, 0));
            }

            recipeComponents.Add("<---COMPONENTS--->");
            recipeComponentColors.Add(new GuiColor(0, 255, 255));
            foreach (var (resref, quantity) in detail.Components)
            {
                var componentName = Cache.GetItemNameByResref(resref);
                recipeComponents.Add($"{quantity}x {componentName}");
                recipeComponentColors.Add(new GuiColor(255, 255, 255));
            }

            RecipeRequirements = recipeRequirements;
            RecipeRequirementColors = recipeRequirementColors;
            RecipeComponents = recipeComponents;
            RecipeComponentColors = recipeComponentColors;
        }

    }
}
