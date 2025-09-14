using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class RecipesDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<RecipesViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Recipes)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 600f)
                .BindTitle(model => model.Title)

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddTextEdit()
                            .SetPlaceholder("Search")
                            .BindValue(model => model.SearchText);

                        row.AddButton()
                            .SetText("X")
                            .SetHeight(35f)
                            .SetWidth(35f)
                            .BindOnClicked(model => model.OnClickClearSearch());

                        row.AddButton()
                            .SetText("Search")
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSearch());
                    });

                    col.AddRow(row =>
                    {
                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedSkillId)
                            .BindOptions(model => model.Skills)
                            .BindIsEnabled(model => model.IsSkillEnabled)
                            .SetWidth(200f);

                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedCategoryId)
                            .BindOptions(model => model.Categories)
                            .BindIsEnabled(model => model.IsSkillSelected)
                            .SetWidth(200f);
                        
                        row.AddButton()
                            .SetText("Select Blueprint")
                            .BindIsVisible(model => model.ShowSelectBlueprint)
                            .SetHeight(35f)
                            .BindOnClicked(model => model.OnClickSelectBlueprint());

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddComboBox()
                            .BindSelectedIndex(model => model.SelectedLevelId)
                            .AddOption("<All Levels>", 0)
                            .AddOption("Level 0-9", 1)
                            .AddOption("Level 10-19", 2)
                            .AddOption("Level 20-29", 3)
                            .AddOption("Level 30-39", 4)
                            .AddOption("Level 40-49", 5)
                            .AddOption("Level 50+", 6)
                            .SetWidth(200f);
                        
                        row.AddCheckBox()
                            .SetText("Show Only Craftable")
                            .BindIsChecked(model => model.ShowOnlyCraftableRecipes)
                            .SetWidth(200f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddColumn(col2 =>
                        {
                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                    {
                                        template.AddCell(cell =>
                                        {
                                            cell.AddToggleButton()
                                                .BindIsToggled(model => model.RecipeToggles)
                                                .BindText(model => model.RecipeNames)
                                                .BindOnClicked(model => model.OnSelectRecipe())
                                                .BindColor(model => model.RecipeColors);
                                        });
                                    })
                                    .BindRowCount(model => model.RecipeNames);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButton()
                                    .SetText("<")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickPreviousPage());

                                row2.AddComboBox()
                                    .BindOptions(model => model.PageNumbers)
                                    .BindSelectedIndex(model => model.SelectedPageIndex);

                                row2.AddButton()
                                    .SetText(">")
                                    .SetWidth(32f)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickNextPage());

                                row2.AddSpacer();
                            });
                        });

                        row.AddColumn(col2 =>
                        {

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddLabel()
                                    .BindText(model => model.RecipeName)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Top)
                                    .SetHeight(26f);
                                row2.AddSpacer();
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddLabel()
                                    .BindText(model => model.RecipeLevel)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Top)
                                    .SetHeight(26f);
                                row2.AddSpacer();
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddLabel()
                                    .BindText(model => model.RecipeEnhancementSlots)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Top)
                                    .SetHeight(26f);
                                row2.AddSpacer();
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddList(template =>
                                {
                                    template.AddCell(cell =>
                                    {
                                        cell.AddLabel()
                                            .BindText(model => model.RecipeDetails)
                                            .BindColor(model => model.RecipeDetailColors)
                                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                                    });
                                })
                                    .BindRowCount(model => model.RecipeDetails);
                            });

                            col2.AddRow(row2 =>
                            {
                                row2.AddSpacer();
                                row2.AddButton()
                                    .BindText(model => model.ActionButtonText)
                                    .BindIsVisible(model => model.IsCraftOrResearchVisible)
                                    .BindIsEnabled(model => model.CanCraftOrResearchRecipe)
                                    .SetHeight(35f)
                                    .BindOnClicked(model => model.OnClickCraftOrResearch());
                                row2.AddSpacer();
                            });
                        });
                    });

                });

            return _builder.Build();
        }
    }
}
