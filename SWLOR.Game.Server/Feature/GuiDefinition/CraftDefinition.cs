using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class CraftDefinition : IGuiWindowDefinition
    {
        private readonly GuiWindowBuilder<CraftViewModel> _builder = new();

        public GuiConstructedWindow BuildWindow()
        {
            _builder.CreateWindow(GuiWindowType.Craft)
                .SetIsResizable(true)
                .SetIsCollapsible(true)
                .SetInitialGeometry(0, 0, 545f, 295.5f)
                .SetTitle("Craft Item")
                .BindIsClosable(model => model.IsClosable)
                .BindOnClosed(model => model.OnCloseWindow())

                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.RecipeName)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.RecipeLevel)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.YourSkill)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Select Enhancements")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .BindIsVisible(model => model.IsEnhancement1Visible);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement1Resref)
                            .BindIsVisible(model => model.IsEnhancement1Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement1())
                            .BindTooltip(model => model.Enhancement1Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement2Resref)
                            .BindIsVisible(model => model.IsEnhancement2Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement2())
                            .BindTooltip(model => model.Enhancement2Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement3Resref)
                            .BindIsVisible(model => model.IsEnhancement3Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement3())
                            .BindTooltip(model => model.Enhancement3Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement4Resref)
                            .BindIsVisible(model => model.IsEnhancement4Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement4())
                            .BindTooltip(model => model.Enhancement4Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement5Resref)
                            .BindIsVisible(model => model.IsEnhancement5Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement5())
                            .BindTooltip(model => model.Enhancement5Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement6Resref)
                            .BindIsVisible(model => model.IsEnhancement6Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement6())
                            .BindTooltip(model => model.Enhancement6Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement7Resref)
                            .BindIsVisible(model => model.IsEnhancement7Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement7())
                            .BindTooltip(model => model.Enhancement7Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddButtonImage()
                            .BindImageResref(model => model.Enhancement8Resref)
                            .BindIsVisible(model => model.IsEnhancement8Visible)
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .BindOnClicked(model => model.OnClickEnhancement8())
                            .BindTooltip(model => model.Enhancement8Tooltip)
                            .SetHeight(32f)
                            .SetWidth(32f);

                        row.AddSpacer();
                    });

                    col.AddRow(row =>
                    {
                        row.AddList(template =>
                        {
                            template.AddCell(cell =>
                            {
                                cell.AddLabel()
                                    .BindText(model => model.RecipeDescription)
                                    .BindColor(model => model.RecipeColors)
                                    .SetHorizontalAlign(NuiHorizontalAlign.Left)
                                    .SetVerticalAlign(NuiVerticalAlign.Middle);
                            });
                        })
                            .BindRowCount(model => model.RecipeDescription);
                    });

                    col.AddRow(row =>
                    {
                        row.AddSpacer();
                        
                        row.AddButton()
                            .BindText(model => model.CraftText)
                            .BindOnClicked(model => model.OnClickManualCraft())
                            .BindIsEnabled(model => model.IsInSetupMode)
                            .SetHeight(35f);

                        row.AddSpacer();
                    });

                })
                
                .AddColumn(col =>
                {
                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.StatusText)
                            .BindColor(model => model.StatusColor)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle)
                            .SetHeight(20f);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.DurabilityText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.DurabilityPercentage)
                            .SetColor(169, 169, 169);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.ProgressText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.ProgressPercentage)
                            .SetColor(50, 205, 50);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.QualityText)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddProgressBar()
                            .BindValue(model => model.QualityPercentage)
                            .SetColor(0, 191, 255);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .BindText(model => model.CP)
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Center)
                            .SetVerticalAlign(NuiVerticalAlign.Middle);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Synthesis Abilities:")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Basic Synthesis [0]")
                            .BindOnClicked(model => model.OnClickBasicSynthesis())
                            .SetTooltip("Increases progress by 10. (90% success rate)")
                            .BindIsEnabled(model => model.IsInCraftMode);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Rapid Synthesis [6]")
                            .BindOnClicked(model => model.OnClickRapidSynthesis())
                            .SetTooltip("Increases progress by 30. (75% success rate)")
                            .BindIsEnabled(model => model.IsRapidSynthesisEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Careful Synthesis [15]")
                            .BindOnClicked(model => model.OnClickCarefulSynthesis())
                            .SetTooltip("Increases progress by 80. (50% success rate)")
                            .BindIsEnabled(model => model.IsCarefulSynthesisEnabled);
                    });

                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Touch Abilities:")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Basic Touch [3]")
                            .BindOnClicked(model => model.OnClickBasicTouch())
                            .SetTooltip("Increases quality by 10. (90% success rate)")
                            .BindIsEnabled(model => model.IsBasicTouchEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Standard Touch [6]")
                            .BindOnClicked(model => model.OnClickStandardTouch())
                            .SetTooltip("Increases quality by 30. (75% success rate)")
                            .BindIsEnabled(model => model.IsStandardTouchEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Precise Touch [15]")
                            .BindOnClicked(model => model.OnClickPreciseTouch())
                            .SetTooltip("Increases quality by 80. (50% success rate)")
                            .BindIsEnabled(model => model.IsPreciseTouchEnabled);
                    });


                    col.AddRow(row =>
                    {
                        row.AddLabel()
                            .SetText("Abilities:")
                            .SetHeight(20f)
                            .SetHorizontalAlign(NuiHorizontalAlign.Left)
                            .SetVerticalAlign(NuiVerticalAlign.Top);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Master's Mend [10]")
                            .BindOnClicked(model => model.OnClickMastersMend())
                            .SetTooltip("Restores item durability by 30.")
                            .BindIsEnabled(model => model.IsMastersMendEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Steady Hand [12]")
                            .BindOnClicked(model => model.OnClickSteadyHand())
                            .SetTooltip("Increases success rate of next synthesis ability to 100%.")
                            .BindIsEnabled(model => model.IsSteadyHandEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Muscle Memory [12]")
                            .BindOnClicked(model => model.OnClickMuscleMemory())
                            .SetTooltip("Increases success rate of next touch ability to 100%.")
                            .BindIsEnabled(model => model.IsMuscleMemoryEnabled);
                    });

                    col.AddRow(row =>
                    {
                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Veneration [8]")
                            .BindOnClicked(model => model.OnClickVeneration())
                            .SetTooltip("Reduces CP cost of Synthesis abilitites by 50% for the next four actions.")
                            .BindIsEnabled(model => model.IsVenerationEnabled);

                        row.AddButton()
                            .SetHeight(30f)
                            .SetText("Waste Not [4]")
                            .BindOnClicked(model => model.OnClickWasteNot())
                            .SetTooltip("Reduces loss of durability by 50% for the next four actions.")
                            .BindIsEnabled(model => model.IsWasteNotEnabled);
                    });
                })
                
                ;
            

            return _builder.Build();
        }
}
}
