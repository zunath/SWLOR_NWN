using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class RecipeDialog : DialogBase
    {
        private class Model
        {
            public SkillType SelectedSkill { get; set; }
            public RecipeCategoryType SelectedCategory { get; set; }
            public RecipeType SelectedRecipe { get; set; }
            public bool IsFabricator { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string CategoryPageId = "CATEGORY_PAGE";
        private const string RecipeListPageId = "RECIPE_LIST_PAGE";
        private const string RecipePageId = "RECIPE_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var model = new Model();

            // This menu can be entered one of two ways:
            //    1.) From the player's rest menu.
            //    2.) From using a crafting fabricator.
            // If the second event happens, we need to skip over the first page.
            var state = Craft.GetPlayerCraftingState(player);
            model.SelectedSkill = state.DeviceSkillType;

            if (model.SelectedSkill != SkillType.Invalid)
            {
                model.IsFabricator = true;
            }

            var builder = new DialogBuilder()
                .WithDataModel(model)
                .AddBackAction(Back)
                .AddEndAction(End);

            if (!model.IsFabricator)
                builder.AddPage(MainPageId, MainPageInit);

            builder.AddPage(CategoryPageId, CategoryPageInit)
                    .AddPage(RecipeListPageId, RecipeListPageInit)
                    .AddPage(RecipePageId, RecipePageInit);

            return builder.Build();
        }

        private void End()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            if (model.IsFabricator)
            {
                AssignCommand(player, () => ActionInteractObject(OBJECT_SELF));
            }
            else
            {
                Craft.ClearPlayerCraftingState(player);
            }
        }

        private void Back(string oldPage, string newPage)
        {
            if (newPage == MainPageId)
            {
                var model = GetDataModel<Model>();
                model.SelectedCategory = RecipeCategoryType.Invalid;
                model.SelectedRecipe = RecipeType.Invalid;

                if (!model.IsFabricator)
                    model.SelectedSkill = SkillType.Invalid;
            }
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();

            page.Header = "Please select a skill.";

            page.AddResponse(Skill.GetSkillDetails(SkillType.Smithery).Name, () =>
            {
                model.SelectedSkill = SkillType.Smithery;
                ChangePage(CategoryPageId);
            });

            page.AddResponse(Skill.GetSkillDetails(SkillType.Fabrication).Name, () =>
            {
                model.SelectedSkill = SkillType.Fabrication;
                ChangePage(CategoryPageId);
            });

            page.AddResponse(Skill.GetSkillDetails(SkillType.FirstAid).Name, () =>
            {
                model.SelectedSkill = SkillType.FirstAid;
                ChangePage(CategoryPageId);
            });

            page.AddResponse(Skill.GetSkillDetails(SkillType.Cybertech).Name, () =>
            {
                model.SelectedSkill = SkillType.Cybertech;
                ChangePage(CategoryPageId);
            });
        }


        private void CategoryPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = "Please select a category.";

            foreach (var (key, value) in Craft.GetRecipeCategoriesBySkill(model.SelectedSkill))
            {
                page.AddResponse(value.Name, () =>
                {
                    model.SelectedCategory = key;
                    ChangePage(RecipeListPageId);
                });
            }
        }

        private void RecipeListPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            page.Header = "Please select a recipe.";

            // Currently in the crafting menu.
            // Display only the recipes the player knows.
            if (model.IsFabricator)
            {
                foreach (var (key, value) in Craft.GetRecipesBySkillAndCategory(model.SelectedSkill, model.SelectedCategory))
                {
                    var itemName = Cache.GetItemNameByResref(value.Resref);
                    if (string.IsNullOrWhiteSpace(itemName))
                    {
                        Log.Write(LogGroup.Error, $"Recipe {key} has an invalid Resref or the name of the item is blank.");
                        continue;
                    }

                    var canCraft = CanPlayerCraftRecipe(key);

                    if (canCraft)
                    {
                        page.AddResponse(ColorToken.Green(itemName), () =>
                        {
                            model.SelectedRecipe = key;
                            ChangePage(RecipePageId);
                        });
                    }
                }
            }
            // Currently in the recipe menu.
            // Display all recipes, highlighting the text in green or red depending on whether the player knows it.
            else
            {
                foreach (var (key, value) in Craft.GetRecipesBySkillAndCategory(model.SelectedSkill, model.SelectedCategory))
                {
                    var itemName = Cache.GetItemNameByResref(value.Resref);
                    if (string.IsNullOrWhiteSpace(itemName))
                    {
                        Log.Write(LogGroup.Error, $"Recipe {key} has an invalid Resref or the name of the item is blank.");
                        continue;
                    }

                    var canCraft = CanPlayerCraftRecipe(key);
                    var optionText = canCraft ? ColorToken.Green(itemName) : ColorToken.Red(itemName);

                    page.AddResponse(optionText, () =>
                    {
                        model.SelectedRecipe = key;
                        ChangePage(RecipePageId);
                    });
                }
            }

        }

        private bool CanPlayerCraftRecipe(RecipeType recipeType)
        {
            var player = GetPC();
            var recipe = Craft.GetRecipe(recipeType);
            if (recipe.Requirements.Count <= 0) return true;

            foreach (var requirement in recipe.Requirements)
            {
                if (!string.IsNullOrWhiteSpace(requirement.CheckRequirements(player)))
                {
                    return false;
                }
            }

            return true;
        }

        private void RecipePageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var meetsRequirements = true;

            string BuildHeader()
            {
                var recipe = Craft.GetRecipe(model.SelectedRecipe);
                var category = Craft.GetCategoryDetail(recipe.Category);
                var skill = Skill.GetSkillDetails(recipe.Skill);
                var itemName = Cache.GetItemNameByResref(recipe.Resref);

                // Recipe quantity and name.
                var header = $"{ColorToken.Green("Recipe:")} {recipe.Quantity}x {itemName} \n";

                // Associated skill
                header += $"{ColorToken.Green("Craft:")} {skill.Name}\n";

                // Associated category
                header += $"{ColorToken.Green("Category:")} {category.Name}\n";

                // Chance to craft
                header += $"{ColorToken.Green("Success Chance:")} {Craft.CalculateChanceToCraft(player, model.SelectedRecipe)}%";

                // List of requirements, if applicable.
                if (recipe.Requirements.Count > 0)
                {
                    header += $"\n{ColorToken.Green("Requirements:")}\n\n";

                    foreach (var req in recipe.Requirements)
                    {
                        // If the player meets the requirement, show it in green. Otherwise show it in red.
                        if (string.IsNullOrWhiteSpace(req.CheckRequirements(player)))
                        {
                            header += $"{ColorToken.Green(req.RequirementText)}\n";
                        }
                        else
                        {
                            header += $"{ColorToken.Red(req.RequirementText)}\n";
                            meetsRequirements = false;
                        }
                    }
                }

                // List of components
                header += $"\n\n{ColorToken.Green("Components:")}\n\n";

                foreach (var (resref, quantity) in recipe.Components)
                {
                    var name = Cache.GetItemNameByResref(resref);
                    header += $"{quantity}x {name}\n";
                }

                return header;
            }

            page.Header = BuildHeader();

            if (model.IsFabricator && meetsRequirements)
            {
                page.AddResponse("Select Recipe", () =>
                {
                    var state = Craft.GetPlayerCraftingState(player);
                    state.SelectedRecipe = model.SelectedRecipe;

                    EndConversation();
                    Player.ForcePlaceableInventoryWindow(player, OBJECT_SELF);
                });
            }
        }
    }
}
